-- Carga de exemplo do catálogo: 10 produtos com variantes, em 4 categorias.
--
-- Idempotente: categoria é reaproveitada pelo slug (criada se não existir); produto cujo slug ou
-- SKU já existe é pulado junto com as variantes. Erro no meio desfaz o bloco inteiro.
--
-- Slug e NomeParaBusca são gravados prontos e seguem GeradorDeSlug e
-- NormalizadorDeTexto.ParaBusca (Ecommerce.Domain/Comum): mudou o nome, recalcule os dois.
-- SKU sempre em maiúsculo (Produto/VarianteDeProduto normalizam assim).
--
-- Rodar como o dono do schema (no dev: ecommerce@localhost:1521/XEPDB1) com o cliente em UTF-8
-- (sqlplus: NLS_LANG=AMERICAN_AMERICA.AL32UTF8; o DBeaver já usa UTF-8), senão os acentos
-- chegam corrompidos.

SET SERVEROUTPUT ON

DECLARE
    v_eletronicos RAW(16);
    v_casa        RAW(16);
    v_moda        RAW(16);
    v_esporte     RAW(16);
    v_produto     RAW(16);
    v_ordem       PLS_INTEGER := 0;

    FUNCTION obter_categoria(p_nome NVARCHAR2, p_slug NVARCHAR2) RETURN RAW IS
        v_id RAW(16);
    BEGIN
        SELECT "Id" INTO v_id FROM "CAT_Categorias" WHERE "Slug" = p_slug;
        RETURN v_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_id := SYS_GUID();
            INSERT INTO "CAT_Categorias" ("Id", "Nome", "Slug") VALUES (v_id, p_nome, p_slug);
            DBMS_OUTPUT.PUT_LINE('Categoria criada: ' || p_slug);
            RETURN v_id;
    END;

    FUNCTION criar_produto(
        p_categoria  RAW,
        p_nome       NVARCHAR2,
        p_nome_busca NVARCHAR2,
        p_slug       NVARCHAR2,
        p_sku        NVARCHAR2,
        p_preco      NUMBER,
        p_descricao  NCLOB) RETURN RAW
    IS
        v_existe PLS_INTEGER;
        v_id     RAW(16);
    BEGIN
        SELECT COUNT(*) INTO v_existe FROM "CAT_Produtos" WHERE "Slug" = p_slug OR "Sku" = p_sku;
        IF v_existe > 0 THEN
            DBMS_OUTPUT.PUT_LINE('Já existe, pulado: ' || p_slug);
            RETURN NULL;
        END IF;

        -- Um segundo de diferença entre cada um: a ordenação "mais recente" fica estável.
        v_ordem := v_ordem + 1;
        v_id := SYS_GUID();

        INSERT INTO "CAT_Produtos"
            ("Id", "CategoriaId", "Nome", "NomeParaBusca", "Slug", "Sku", "Descricao", "Preco", "Ativo", "CriadoEmUtc")
        VALUES
            (v_id, p_categoria, p_nome, p_nome_busca, p_slug, p_sku, p_descricao, p_preco, 1,
             SYS_EXTRACT_UTC(SYSTIMESTAMP) - NUMTODSINTERVAL(v_ordem, 'SECOND'));

        DBMS_OUTPUT.PUT_LINE('Produto criado: ' || p_slug);
        RETURN v_id;
    END;

    PROCEDURE criar_variante(p_produto RAW, p_nome NVARCHAR2, p_sku NVARCHAR2, p_preco NUMBER, p_estoque PLS_INTEGER) IS
    BEGIN
        IF p_produto IS NULL THEN
            RETURN;
        END IF;

        INSERT INTO "CAT_VariantesDeProduto" ("Id", "ProdutoId", "Nome", "Sku", "Preco", "Estoque", "Ativo")
        VALUES (SYS_GUID(), p_produto, p_nome, p_sku, p_preco, p_estoque, 1);
    END;
BEGIN
    v_eletronicos := obter_categoria(N'Eletrônicos', N'eletronicos');
    v_casa        := obter_categoria(N'Casa e Decoração', N'casa-e-decoracao');
    v_moda        := obter_categoria(N'Moda', N'moda');
    v_esporte     := obter_categoria(N'Esporte e Lazer', N'esporte-e-lazer');

    -- Eletrônicos
    v_produto := criar_produto(v_eletronicos, N'Carregador Portátil 20.000mAh', N'CARREGADOR PORTATIL 20.000MAH',
        N'carregador-portatil-20-000mah', N'ELE-POWERBANK-20K', 149.90,
        N'Power bank com carregamento rápido e duas saídas USB-C.');
    criar_variante(v_produto, N'Padrão', N'ELE-POWERBANK-20K-UN', 149.90, 40);

    v_produto := criar_produto(v_eletronicos, N'Caixa de Som Bluetooth', N'CAIXA DE SOM BLUETOOTH',
        N'caixa-de-som-bluetooth', N'ELE-CAIXA-SOM-BT', 199.90,
        N'Caixa de som portátil à prova d''água, 12 horas de bateria.');
    criar_variante(v_produto, N'Preta', N'ELE-CAIXA-SOM-BT-PRETA', 199.90, 20);
    criar_variante(v_produto, N'Azul', N'ELE-CAIXA-SOM-BT-AZUL', 199.90, 15);

    -- Casa e Decoração
    v_produto := criar_produto(v_casa, N'Kit Almofadas Decorativas', N'KIT ALMOFADAS DECORATIVAS',
        N'kit-almofadas-decorativas', N'CASA-ALMOFADAS-KIT4', 129.90,
        N'Conjunto com 4 almofadas em tecido suede, diversas cores.');
    criar_variante(v_produto, N'Padrão', N'CASA-ALMOFADAS-KIT4-UN', 129.90, 25);

    v_produto := criar_produto(v_casa, N'Vaso Cerâmico Decorativo', N'VASO CERAMICO DECORATIVO',
        N'vaso-ceramico-decorativo', N'CASA-VASO-CERAMICA', 69.90,
        N'Vaso feito à mão, acabamento fosco, ideal para plantas pequenas.');
    criar_variante(v_produto, N'Padrão', N'CASA-VASO-CERAMICA-UN', 69.90, 30);

    -- Moda
    v_produto := criar_produto(v_moda, N'Camiseta Básica de Algodão', N'CAMISETA BASICA DE ALGODAO',
        N'camiseta-basica-de-algodao', N'MODA-CAMISETA', 49.90,
        N'Camiseta 100% algodão, corte unissex.');
    criar_variante(v_produto, N'P', N'MODA-CAMISETA-P', 49.90, 20);
    criar_variante(v_produto, N'M', N'MODA-CAMISETA-M', 49.90, 25);
    criar_variante(v_produto, N'G', N'MODA-CAMISETA-G', 49.90, 20);

    v_produto := criar_produto(v_moda, N'Jaqueta Corta-Vento', N'JAQUETA CORTA-VENTO',
        N'jaqueta-corta-vento', N'MODA-JAQUETA-CV', 189.90,
        N'Impermeável, leve e dobrável — cabe em qualquer mochila.');
    criar_variante(v_produto, N'M', N'MODA-JAQUETA-CV-M', 189.90, 10);
    criar_variante(v_produto, N'G', N'MODA-JAQUETA-CV-G', 189.90, 10);

    v_produto := criar_produto(v_moda, N'Tênis Casual Urbano', N'TENIS CASUAL URBANO',
        N'tenis-casual-urbano', N'MODA-TENIS-URBANO', 219.90,
        N'Solado em EVA, cabedal respirável, confortável o dia todo.');
    criar_variante(v_produto, N'39', N'MODA-TENIS-URBANO-39', 219.90, 8);
    criar_variante(v_produto, N'40', N'MODA-TENIS-URBANO-40', 219.90, 8);
    criar_variante(v_produto, N'41', N'MODA-TENIS-URBANO-41', 219.90, 8);
    -- Esgotado de propósito: exemplo de opção indisponível (usado pelo e2e do frontend).
    criar_variante(v_produto, N'42', N'MODA-TENIS-URBANO-42', 219.90, 0);

    -- Esporte e Lazer
    v_produto := criar_produto(v_esporte, N'Tapete de Yoga Antiderrapante', N'TAPETE DE YOGA ANTIDERRAPANTE',
        N'tapete-de-yoga-antiderrapante', N'ESP-TAPETE-YOGA', 99.90,
        N'Espessura de 6mm, material atóxico, com alça de transporte.');
    criar_variante(v_produto, N'Padrão', N'ESP-TAPETE-YOGA-UN', 99.90, 45);

    v_produto := criar_produto(v_esporte, N'Garrafa Térmica 1 Litro', N'GARRAFA TERMICA 1 LITRO',
        N'garrafa-termica-1-litro', N'ESP-GARRAFA-1L', 79.90,
        N'Mantém a temperatura por até 12 horas, aço inoxidável.');
    criar_variante(v_produto, N'Padrão', N'ESP-GARRAFA-1L-UN', 79.90, 60);

    v_produto := criar_produto(v_esporte, N'Kit Halteres Ajustáveis', N'KIT HALTERES AJUSTAVEIS',
        N'kit-halteres-ajustaveis', N'ESP-HALTERES-KIT', 349.90,
        N'Par de halteres com peso ajustável de 2 a 10kg cada.');
    criar_variante(v_produto, N'Padrão', N'ESP-HALTERES-KIT-UN', 349.90, 15);

    COMMIT;
END;
/
