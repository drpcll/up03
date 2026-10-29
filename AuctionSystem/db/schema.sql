-- =====================================================================
--  АИС "Аукционы" -- схема базы данных
--  СУБД: SQL Server 2022
--  Именование: префикс UP03_ + латинская транслитерация (Телефон -> Telephone)
--  Скрипт идемпотентный: можно выполнять повторно, старые таблицы DROP-аются
-- =====================================================================

SET NOCOUNT ON;

-- ---------------------------------------------------------------------
-- Удаление в обратном порядке зависимостей
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.UP03_RezultatyTorgov', 'U') IS NOT NULL DROP TABLE dbo.UP03_RezultatyTorgov;
IF OBJECT_ID('dbo.UP03_Loty',            'U') IS NOT NULL DROP TABLE dbo.UP03_Loty;
IF OBJECT_ID('dbo.UP03_Auktsiony',       'U') IS NOT NULL DROP TABLE dbo.UP03_Auktsiony;
IF OBJECT_ID('dbo.UP03_Pokupateli',      'U') IS NOT NULL DROP TABLE dbo.UP03_Pokupateli;
IF OBJECT_ID('dbo.UP03_Prodavtsy',       'U') IS NOT NULL DROP TABLE dbo.UP03_Prodavtsy;
IF OBJECT_ID('dbo.UP03_Sotrudniki',      'U') IS NOT NULL DROP TABLE dbo.UP03_Sotrudniki;
IF OBJECT_ID('dbo.UP03_Role',            'U') IS NOT NULL DROP TABLE dbo.UP03_Role;

-- =====================================================================
-- 1. Роль -- справочник ролей пользователей
-- =====================================================================
CREATE TABLE dbo.UP03_Role
(
    IDRole INT IDENTITY(1,1) NOT NULL,
    Tip    VARCHAR(70)        NOT NULL,
    CONSTRAINT PK_UP03_Role PRIMARY KEY (IDRole),
    CONSTRAINT UQ_UP03_Role_Tip UNIQUE (Tip)
);

-- =====================================================================
-- 2. Сотрудники -- 1:* к UP03_Role
-- =====================================================================
CREATE TABLE dbo.UP03_Sotrudniki
(
    IDSotrudnika INT IDENTITY(1,1) NOT NULL,
    Familiya     VARCHAR(100)       NOT NULL,
    Imya         VARCHAR(100)       NOT NULL,
    Otchestvo    VARCHAR(100)       NULL,
    Login        VARCHAR(50)        NOT NULL,
    Parol        VARCHAR(256)       NOT NULL,
    Sol          VARCHAR(128)       NOT NULL,
    IDRole       INT                NOT NULL,
    Telefon      VARCHAR(20)        NULL,
    Email        VARCHAR(100)       NULL,
    DataPrioma   DATE               NOT NULL
        CONSTRAINT DF_UP03_Sotrudniki_DataPrioma DEFAULT (CAST(GETDATE() AS DATE)),
    CONSTRAINT PK_UP03_Sotrudniki PRIMARY KEY (IDSotrudnika),
    CONSTRAINT UQ_UP03_Sotrudniki_Login UNIQUE (Login),
    CONSTRAINT FK_UP03_Sotrudniki_Role FOREIGN KEY (IDRole)
        REFERENCES dbo.UP03_Role (IDRole)
);

-- =====================================================================
-- 3. Продавцы -- владельцы лотов
-- =====================================================================
CREATE TABLE dbo.UP03_Prodavtsy
(
    IDProdavca      INT IDENTITY(1,1) NOT NULL,
    Familiya        VARCHAR(100)       NOT NULL,
    Imya            VARCHAR(100)       NOT NULL,
    Otchestvo       VARCHAR(100)       NULL,
    Telefon         VARCHAR(20)        NULL,
    Email           VARCHAR(MAX)       NULL,
    Adres           VARCHAR(MAX)       NULL,
    Login           VARCHAR(50)        NOT NULL,
    Parol           VARCHAR(256)       NOT NULL,
    Sol             VARCHAR(128)       NOT NULL,
    DataRegistracii DATE               NOT NULL
        CONSTRAINT DF_UP03_Prodavtsy_DataRegistracii DEFAULT (CAST(GETDATE() AS DATE)),
    CONSTRAINT PK_UP03_Prodavtsy PRIMARY KEY (IDProdavca),
    CONSTRAINT UQ_UP03_Prodavtsy_Login UNIQUE (Login)
);

-- =====================================================================
-- 4. Покупатели
-- =====================================================================
CREATE TABLE dbo.UP03_Pokupateli
(
    IDPokupatelya   INT IDENTITY(1,1) NOT NULL,
    Familiya        VARCHAR(100)       NOT NULL,
    Imya            VARCHAR(100)       NOT NULL,
    Otchestvo       VARCHAR(100)       NULL,
    Telefon         VARCHAR(20)        NULL,
    Email           VARCHAR(100)       NULL,
    Adres           VARCHAR(200)       NULL,
    Login           VARCHAR(50)        NOT NULL,
    Parol           VARCHAR(256)       NOT NULL,
    Sol             VARCHAR(128)       NOT NULL,
    DataRegistracii DATE               NOT NULL
        CONSTRAINT DF_UP03_Pokupateli_DataRegistracii DEFAULT (CAST(GETDATE() AS DATE)),
    CONSTRAINT PK_UP03_Pokupateli PRIMARY KEY (IDPokupatelya),
    CONSTRAINT UQ_UP03_Pokupateli_Login UNIQUE (Login)
);

-- =====================================================================
-- 5. Аукционы -- 1:* к UP03_Sotrudniki
-- =====================================================================
CREATE TABLE dbo.UP03_Auktsiony
(
    IDAukciona        INT IDENTITY(1,1) NOT NULL,
    Nazvanie          VARCHAR(150)       NOT NULL,
    DataProvedeniya   DATE               NOT NULL,
    VremyaProvedeniya TIME(0)            NOT NULL,
    MestoProvedeniya  VARCHAR(200)       NULL,
    Specifika         VARCHAR(200)       NULL,
    Foto              VARCHAR(260)       NULL,
    Status            VARCHAR(20)        NOT NULL
        CONSTRAINT DF_UP03_Auktsiony_Status DEFAULT (N'Запланирован'),
    IDSotrudnika      INT                NOT NULL,
    CONSTRAINT PK_UP03_Auktsiony PRIMARY KEY (IDAukciona),
    CONSTRAINT CK_UP03_Auktsiony_Status
        CHECK (Status IN (N'Запланирован', N'Идёт', N'Завершён')),
    CONSTRAINT FK_UP03_Auktsiony_Sotrudniki FOREIGN KEY (IDSotrudnika)
        REFERENCES dbo.UP03_Sotrudniki (IDSotrudnika)
);

-- =====================================================================
-- 6. Лоты -- 1:* к UP03_Auktsiony, 1:* к UP03_Prodavtsy
--    Номер лота уникален в пределах аукциона (п. 1.1 задача 2)
-- =====================================================================
CREATE TABLE dbo.UP03_Loty
(
    IDLota         INT IDENTITY(1,1) NOT NULL,
    NomerLota      INT                NOT NULL,
    Naimenovanie   VARCHAR(200)       NOT NULL,
    Opisanie       VARCHAR(MAX)       NULL,
    StartovayaCena DECIMAL(12,2)      NOT NULL,
    Sostoyanie     VARCHAR(50)        NULL,
    Foto           VARCHAR(260)       NULL,
    Status         VARCHAR(20)        NOT NULL
        CONSTRAINT DF_UP03_Loty_Status DEFAULT (N'Запланирован'),
    IDAukciona     INT                NOT NULL,
    IDProdavca     INT                NOT NULL,
    CONSTRAINT PK_UP03_Loty PRIMARY KEY (IDLota),
    CONSTRAINT UQ_UP03_Loty_Aukcion_Nomer UNIQUE (IDAukciona, NomerLota),
    CONSTRAINT CK_UP03_Loty_Nomer  CHECK (NomerLota > 0),
    CONSTRAINT CK_UP03_Loty_Cena   CHECK (StartovayaCena >= 0),
    CONSTRAINT CK_UP03_Loty_Status CHECK (Status IN
        (N'Запланирован', N'Выставлен', N'Продан', N'Не продан')),
    CONSTRAINT FK_UP03_Loty_Auktsiony FOREIGN KEY (IDAukciona)
        REFERENCES dbo.UP03_Auktsiony (IDAukciona),
    CONSTRAINT FK_UP03_Loty_Prodavtsy FOREIGN KEY (IDProdavca)
        REFERENCES dbo.UP03_Prodavtsy (IDProdavca)
);

-- =====================================================================
-- 7. Результаты торгов -- 1:1 к UP03_Loty, 1:* к UP03_Pokupateli
--    IDPokupatelya NULL, если лот не продан
-- =====================================================================
CREATE TABLE dbo.UP03_RezultatyTorgov
(
    IDRezultata     INT IDENTITY(1,1) NOT NULL,
    IDLota          INT                NOT NULL,
    PriznakProdazhi BIT                NOT NULL,
    FinalnayaCena   DECIMAL(12,2)      NULL,
    SummaKommissii  DECIMAL(12,2)      NULL,
    IDPokupatelya   INT                NULL,
    DataProdazhi    DATE               NULL,
    CONSTRAINT PK_UP03_RezultatyTorgov PRIMARY KEY (IDRezultata),
    CONSTRAINT UQ_UP03_RezultatyTorgov_Lot UNIQUE (IDLota),
    CONSTRAINT CK_UP03_RezultatyTorgov_Cena
        CHECK (FinalnayaCena IS NULL OR FinalnayaCena >= 0),
    CONSTRAINT CK_UP03_RezultatyTorgov_Prodan
        CHECK (
            (PriznakProdazhi = 1 AND IDPokupatelya IS NOT NULL AND DataProdazhi IS NOT NULL)
            OR
            (PriznakProdazhi = 0 AND IDPokupatelya IS NULL)
        ),
    CONSTRAINT FK_UP03_RezultatyTorgov_Loty FOREIGN KEY (IDLota)
        REFERENCES dbo.UP03_Loty (IDLota),
    CONSTRAINT FK_UP03_RezultatyTorgov_Pokupatel FOREIGN KEY (IDPokupatelya)
        REFERENCES dbo.UP03_Pokupateli (IDPokupatelya)
);

-- =====================================================================
--  Справочник ролей (4 роли по п. 1.3 документа)
-- =====================================================================
INSERT INTO dbo.UP03_Role (Tip) VALUES
    (N'Администратор'),
    (N'Администратор аукциона'),
    (N'Продавец'),
    (N'Покупатель');

PRINT 'Схема АИС "Аукционы" создана: 7 таблиц, 4 роли.';
