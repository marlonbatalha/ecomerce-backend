-- Carga sintética de produtos para teste de desempenho (catálogo, busca, paginação).
--
-- Gera v_quantidade produtos com 1 a 3 variantes cada, nas 4 categorias do seed de exemplo
-- (sql/seed-produtos-exemplo.sql, que precisa ter rodado antes). Nomes como "Camiseta Premium 007":
-- o tipo se repete entre produtos, então a busca por termo devolve vários resultados.
--
-- Idempotente: SKU com prefixo PERF-<número>; produto que já existe é pulado. Pra remover tudo:
--   DELETE FROM "CAT_VariantesDeProduto" WHERE "Sku" LIKE 'PERF-%';
--   DELETE FROM "CAT_Produtos" WHERE "Sku" LIKE 'PERF-%';
--
-- Determinístico (DBMS_RANDOM.SEED fixo): rodar de novo num banco limpo gera os mesmos dados.
-- Slug e NomeParaBusca seguem GeradorDeSlug / NormalizadorDeTexto.ParaBusca do backend.
-- Rodar com o cliente em UTF-8 (sqlplus: NLS_LANG=AMERICAN_AMERICA.AL32UTF8).

SET SERVEROUTPUT ON

DECLARE
    v_quantidade CONSTANT PLS_INTEGER := 90;

    TYPE t_textos IS TABLE OF NVARCHAR2(60);
    -- Tipo de produto e a categoria (slug) a que pertence — mesma posição nas duas listas.
    v_tipos      t_textos := t_textos(
        N'Camiseta', N'Moletom', N'Calça Jeans', N'Boné', N'Tênis Corrida', N'Jaqueta', N'Bermuda',
        N'Fone Bluetooth', N'Mouse Sem Fio', N'Teclado Mecânico', N'Carregador USB-C', N'Smartwatch', N'Caixa de Som',
        N'Luminária', N'Almofada', N'Vaso', N'Tapete', N'Quadro Decorativo', N'Jogo de Toalhas',
        N'Garrafa Térmica', N'Bola de Futebol', N'Tapete de Yoga', N'Corda de Pular', N'Mochila Esportiva');
    v_categorias t_textos := t_textos(
        N'moda', N'moda', N'moda', N'moda', N'moda', N'moda', N'moda',
        N'eletronicos', N'eletronicos', N'eletronicos', N'eletronicos', N'eletronicos', N'eletronicos',
        N'casa-e-decoracao', N'casa-e-decoracao', N'casa-e-decoracao', N'casa-e-decoracao', N'casa-e-decoracao', N'casa-e-decoracao',
        N'esporte-e-lazer', N'esporte-e-lazer', N'esporte-e-lazer', N'esporte-e-lazer', N'esporte-e-lazer');
    v_linhas     t_textos := t_textos(
        N'Premium', N'Essencial', N'Pro', N'Slim', N'Clássico', N'Urbano', N'Confort', N'Max', N'Eco', N'Plus');
    v_tamanhos   t_textos := t_textos(N'P', N'M', N'G');

    v_tipo       PLS_INTEGER;
    v_nome       NVARCHAR2(150);
    v_busca      NVARCHAR2(150);
    v_slug       NVARCHAR2(150);
    v_sku        NVARCHAR2(50);
    v_preco      NUMBER(18, 2);
    v_produto    RAW(16);
    v_categoria  RAW(16);
    v_variantes  PLS_INTEGER;
    v_existe     PLS_INTEGER;
    v_criados    PLS_INTEGER := 0;

    -- Mesma regra do NormalizadorDeTexto.RemoverAcentos (só os acentos do português).
    FUNCTION sem_acento(p_texto NVARCHAR2) RETURN NVARCHAR2 IS
    BEGIN
        RETURN TRANSLATE(p_texto,
            N'ÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇáàâãäéèêëíìîïóòôõöúùûüç',
            N'AAAAAEEEEIIIIOOOOOUUUUCaaaaaeeeeiiiiooooouuuuc');
    END;
BEGIN
    DBMS_RANDOM.SEED(42);

    FOR i IN 1 .. v_quantidade LOOP
        v_sku := N'PERF-' || LPAD(i, 4, '0');
        SELECT COUNT(*) INTO v_existe FROM "CAT_Produtos" WHERE "Sku" = v_sku;

        -- Sorteia sempre (mesmo quando pula) pra sequência aleatória não depender do que já existe.
        v_tipo := MOD(i - 1, v_tipos.COUNT) + 1;
        v_preco := ROUND(DBMS_RANDOM.VALUE(19, 999)) + 0.90;
        v_variantes := CASE WHEN v_categorias(v_tipo) = N'moda' THEN 3 ELSE TRUNC(DBMS_RANDOM.VALUE(1, 3)) END;

        IF v_existe = 0 THEN
            v_nome := v_tipos(v_tipo) || N' ' || v_linhas(MOD(i * 7, v_linhas.COUNT) + 1) || N' ' || LPAD(i, 3, '0');
            v_busca := REGEXP_REPLACE(UPPER(sem_acento(v_nome)), '\s+', ' ');
            v_slug := TRIM(BOTH '-' FROM REGEXP_REPLACE(LOWER(sem_acento(v_nome)), '[^a-z0-9]+', '-'));

            SELECT "Id" INTO v_categoria FROM "CAT_Categorias" WHERE "Slug" = v_categorias(v_tipo);

            v_produto := SYS_GUID();
            INSERT INTO "CAT_Produtos"
                ("Id", "CategoriaId", "Nome", "NomeParaBusca", "Slug", "Sku", "Descricao", "Preco", "Ativo", "CriadoEmUtc")
            VALUES
                (v_produto, v_categoria, v_nome, v_busca, v_slug, v_sku,
                 N'Produto gerado para teste de desempenho do catálogo.', v_preco, 1,
                 SYS_EXTRACT_UTC(SYSTIMESTAMP) - NUMTODSINTERVAL(i, 'MINUTE'));

            FOR v IN 1 .. v_variantes LOOP
                INSERT INTO "CAT_VariantesDeProduto" ("Id", "ProdutoId", "Nome", "Sku", "Preco", "Estoque", "Ativo")
                VALUES (
                    SYS_GUID(), v_produto,
                    CASE WHEN v_variantes = 3 THEN v_tamanhos(v) ELSE N'Padrão' END,
                    v_sku || N'-' || v,
                    v_preco,
                    -- ~10% das variantes esgotadas: produto sem nenhuma em estoque some da busca pública.
                    CASE WHEN DBMS_RANDOM.VALUE < 0.1 THEN 0 ELSE TRUNC(DBMS_RANDOM.VALUE(1, 60)) END,
                    1);
            END LOOP;

            v_criados := v_criados + 1;
        END IF;
    END LOOP;

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Produtos criados: ' || v_criados || ' (de ' || v_quantidade || ')');
END;
/
