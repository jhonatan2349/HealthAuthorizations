IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE SEQUENCE [AutorizacionesHiLo] START WITH 1 INCREMENT BY 10 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [Contratantes] (
        [Id] int NOT NULL IDENTITY,
        [Documento] varchar(15) NOT NULL,
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        [Codigo] varchar(20) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Contratantes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [Intermediarios] (
        [Id] int NOT NULL IDENTITY,
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        [Codigo] varchar(20) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Intermediarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [TiposPrestadores] (
        [Id] int NOT NULL IDENTITY,
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        [Codigo] varchar(20) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_TiposPrestadores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [TiposServicios] (
        [Id] int NOT NULL IDENTITY,
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        [Codigo] varchar(20) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_TiposServicios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [Afiliados] (
        [Id] int NOT NULL IDENTITY,
        [NumeroAfiliado] varchar(20) NOT NULL,
        [Documento] varchar(15) NOT NULL,
        [Nombres] nvarchar(100) NOT NULL,
        [Apellidos] nvarchar(100) NOT NULL,
        [FechaNacimiento] date NOT NULL,
        [ContratanteId] int NOT NULL,
        [IntermediarioId] int NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        CONSTRAINT [PK_Afiliados] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Afiliados_Contratantes_ContratanteId] FOREIGN KEY ([ContratanteId]) REFERENCES [Contratantes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Afiliados_Intermediarios_IntermediarioId] FOREIGN KEY ([IntermediarioId]) REFERENCES [Intermediarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [Prestadores] (
        [Id] int NOT NULL IDENTITY,
        [Documento] varchar(15) NOT NULL,
        [TipoPrestadorId] int NOT NULL,
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        [Codigo] varchar(20) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Prestadores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prestadores_TiposPrestadores_TipoPrestadorId] FOREIGN KEY ([TipoPrestadorId]) REFERENCES [TiposPrestadores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [Autorizaciones] (
        [Id] bigint NOT NULL,
        [Numero] AS 'AUT-' + RIGHT('0000000000' + CAST([Id] AS varchar(19)), 10) PERSISTED,
        [AfiliadoId] int NOT NULL,
        [PrestadorId] int NOT NULL,
        [ContratanteId] int NOT NULL,
        [IntermediarioId] int NULL,
        [FechaServicio] date NOT NULL,
        [Diagnostico] nvarchar(250) NULL,
        [Observaciones] nvarchar(1000) NULL,
        [Estado] tinyint NOT NULL,
        [FechaInactivacion] datetimeoffset NULL,
        [MotivoInactivacion] nvarchar(500) NULL,
        [MontoReclamado] decimal(18,2) NOT NULL,
        [MontoArs] decimal(18,2) NOT NULL,
        [MontoAfiliado] decimal(18,2) NOT NULL,
        [ClaveIdempotencia] varchar(64) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreadoEn] datetimeoffset NOT NULL,
        [CreadoPor] nvarchar(100) NOT NULL,
        [ModificadoEn] datetimeoffset NULL,
        [ModificadoPor] nvarchar(100) NULL,
        CONSTRAINT [PK_Autorizaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Autorizaciones_Estado] CHECK ([Estado] IN (1, 2)),
        CONSTRAINT [CK_Autorizaciones_Inactivacion] CHECK (([Estado] = 1 AND [FechaInactivacion] IS NULL AND [MotivoInactivacion] IS NULL) OR ([Estado] = 2 AND [FechaInactivacion] IS NOT NULL AND [MotivoInactivacion] IS NOT NULL)),
        CONSTRAINT [CK_Autorizaciones_Montos] CHECK ([MontoReclamado] > 0 AND [MontoArs] >= 0 AND [MontoAfiliado] >= 0 AND [MontoArs] + [MontoAfiliado] = [MontoReclamado]),
        CONSTRAINT [FK_Autorizaciones_Afiliados_AfiliadoId] FOREIGN KEY ([AfiliadoId]) REFERENCES [Afiliados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Autorizaciones_Contratantes_ContratanteId] FOREIGN KEY ([ContratanteId]) REFERENCES [Contratantes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Autorizaciones_Intermediarios_IntermediarioId] FOREIGN KEY ([IntermediarioId]) REFERENCES [Intermediarios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Autorizaciones_Prestadores_PrestadorId] FOREIGN KEY ([PrestadorId]) REFERENCES [Prestadores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE TABLE [AutorizacionesDetalle] (
        [Id] bigint NOT NULL IDENTITY,
        [AutorizacionId] bigint NOT NULL,
        [TipoServicioId] int NOT NULL,
        [Descripcion] nvarchar(250) NOT NULL,
        [MontoReclamado] decimal(18,2) NOT NULL,
        [PorcentajeCobertura] decimal(5,2) NOT NULL,
        [MontoArs] decimal(18,2) NOT NULL,
        [MontoAfiliado] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_AutorizacionesDetalle] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AutorizacionesDetalle_Montos] CHECK ([MontoReclamado] > 0 AND [MontoArs] >= 0 AND [MontoAfiliado] >= 0 AND [MontoArs] + [MontoAfiliado] = [MontoReclamado]),
        CONSTRAINT [CK_AutorizacionesDetalle_Porcentaje] CHECK ([PorcentajeCobertura] BETWEEN 0 AND 100),
        CONSTRAINT [FK_AutorizacionesDetalle_Autorizaciones_AutorizacionId] FOREIGN KEY ([AutorizacionId]) REFERENCES [Autorizaciones] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AutorizacionesDetalle_TiposServicios_TipoServicioId] FOREIGN KEY ([TipoServicioId]) REFERENCES [TiposServicios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'Documento', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[Contratantes]'))
        SET IDENTITY_INSERT [Contratantes] ON;
    EXEC(N'INSERT INTO [Contratantes] ([Id], [Activo], [Codigo], [CreadoEn], [CreadoPor], [Documento], [ModificadoEn], [ModificadoPor], [Nombre])
    VALUES (1, CAST(1 AS bit), ''CTR-001'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000001'', NULL, NULL, N''Constructora Los Andes SRL''),
    (2, CAST(1 AS bit), ''CTR-002'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000002'', NULL, NULL, N''Banco Comercial del Este SA''),
    (3, CAST(1 AS bit), ''CTR-003'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000003'', NULL, NULL, N''Distribuidora Alimentos del Norte SRL''),
    (4, CAST(1 AS bit), ''CTR-004'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''00100000099'', NULL, NULL, N''Plan Individual'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'Documento', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[Contratantes]'))
        SET IDENTITY_INSERT [Contratantes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[Intermediarios]'))
        SET IDENTITY_INSERT [Intermediarios] ON;
    EXEC(N'INSERT INTO [Intermediarios] ([Id], [Activo], [Codigo], [CreadoEn], [CreadoPor], [ModificadoEn], [ModificadoPor], [Nombre])
    VALUES (1, CAST(1 AS bit), ''INT-001'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Corredora de Seguros del Caribe''),
    (2, CAST(1 AS bit), ''INT-002'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Asesores de Salud Quisqueya''),
    (3, CAST(1 AS bit), ''INT-003'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Agentes Unidos de Seguros'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[Intermediarios]'))
        SET IDENTITY_INSERT [Intermediarios] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[TiposPrestadores]'))
        SET IDENTITY_INSERT [TiposPrestadores] ON;
    EXEC(N'INSERT INTO [TiposPrestadores] ([Id], [Activo], [Codigo], [CreadoEn], [CreadoPor], [ModificadoEn], [ModificadoPor], [Nombre])
    VALUES (1, CAST(1 AS bit), ''MED'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Médico''),
    (2, CAST(1 AS bit), ''FAR'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Farmacia''),
    (3, CAST(1 AS bit), ''CLI'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Clínica''),
    (4, CAST(1 AS bit), ''LAB'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Laboratorio'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[TiposPrestadores]'))
        SET IDENTITY_INSERT [TiposPrestadores] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[TiposServicios]'))
        SET IDENTITY_INSERT [TiposServicios] ON;
    EXEC(N'INSERT INTO [TiposServicios] ([Id], [Activo], [Codigo], [CreadoEn], [CreadoPor], [ModificadoEn], [ModificadoPor], [Nombre])
    VALUES (1, CAST(1 AS bit), ''CON'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Consulta''),
    (2, CAST(1 AS bit), ''EME'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Emergencia''),
    (3, CAST(1 AS bit), ''HOS'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Hospitalización''),
    (4, CAST(1 AS bit), ''LAB'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Laboratorio''),
    (5, CAST(1 AS bit), ''IMG'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', NULL, NULL, N''Imágenes'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'ModificadoEn', N'ModificadoPor', N'Nombre') AND [object_id] = OBJECT_ID(N'[TiposServicios]'))
        SET IDENTITY_INSERT [TiposServicios] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Apellidos', N'ContratanteId', N'CreadoEn', N'CreadoPor', N'Documento', N'FechaNacimiento', N'IntermediarioId', N'ModificadoEn', N'ModificadoPor', N'Nombres', N'NumeroAfiliado') AND [object_id] = OBJECT_ID(N'[Afiliados]'))
        SET IDENTITY_INSERT [Afiliados] ON;
    EXEC(N'INSERT INTO [Afiliados] ([Id], [Activo], [Apellidos], [ContratanteId], [CreadoEn], [CreadoPor], [Documento], [FechaNacimiento], [IntermediarioId], [ModificadoEn], [ModificadoPor], [Nombres], [NumeroAfiliado])
    VALUES (1, CAST(1 AS bit), N''Rodríguez Peña'', 1, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000001'', ''1985-03-14'', 1, NULL, NULL, N''María'', ''AF-000001''),
    (2, CAST(1 AS bit), N''Fernández Díaz'', 1, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000002'', ''1979-07-02'', 1, NULL, NULL, N''José'', ''AF-000002''),
    (3, CAST(1 AS bit), N''Santos Reyes'', 2, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000003'', ''1992-11-23'', 2, NULL, NULL, N''Ana'', ''AF-000003''),
    (4, CAST(1 AS bit), N''Castillo Mejía'', 2, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000004'', ''1968-01-30'', 2, NULL, NULL, N''Luis'', ''AF-000004''),
    (5, CAST(1 AS bit), N''Jiménez Torres'', 2, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000005'', ''2001-05-09'', NULL, NULL, NULL, N''Carmen'', ''AF-000005''),
    (6, CAST(1 AS bit), N''Guzmán Núñez'', 3, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000006'', ''1975-09-17'', 3, NULL, NULL, N''Pedro'', ''AF-000006''),
    (7, CAST(1 AS bit), N''Vargas Cruz'', 3, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000007'', ''1988-12-05'', 3, NULL, NULL, N''Rosa'', ''AF-000007''),
    (8, CAST(1 AS bit), N''Herrera Polanco'', 3, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000008'', ''1995-04-21'', NULL, NULL, NULL, N''Miguel'', ''AF-000008''),
    (9, CAST(1 AS bit), N''Batista Familia'', 4, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000009'', ''1960-08-11'', 1, NULL, NULL, N''Elena'', ''AF-000009''),
    (10, CAST(1 AS bit), N''De la Cruz Ortiz'', 4, ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''40200000010'', ''2010-02-28'', NULL, NULL, NULL, N''Ramón'', ''AF-000010'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Apellidos', N'ContratanteId', N'CreadoEn', N'CreadoPor', N'Documento', N'FechaNacimiento', N'IntermediarioId', N'ModificadoEn', N'ModificadoPor', N'Nombres', N'NumeroAfiliado') AND [object_id] = OBJECT_ID(N'[Afiliados]'))
        SET IDENTITY_INSERT [Afiliados] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'Documento', N'ModificadoEn', N'ModificadoPor', N'Nombre', N'TipoPrestadorId') AND [object_id] = OBJECT_ID(N'[Prestadores]'))
        SET IDENTITY_INSERT [Prestadores] ON;
    EXEC(N'INSERT INTO [Prestadores] ([Id], [Activo], [Codigo], [CreadoEn], [CreadoPor], [Documento], [ModificadoEn], [ModificadoPor], [Nombre], [TipoPrestadorId])
    VALUES (1, CAST(1 AS bit), ''PRE-001'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''00100000101'', NULL, NULL, N''Dr. Carlos Martínez - Medicina Interna'', 1),
    (2, CAST(1 AS bit), ''PRE-002'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''00100000102'', NULL, NULL, N''Dra. Laura Gómez - Pediatría'', 1),
    (3, CAST(1 AS bit), ''PRE-003'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000101'', NULL, NULL, N''Farmacia Salud Total'', 2),
    (4, CAST(1 AS bit), ''PRE-004'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000102'', NULL, NULL, N''Farmacia del Pueblo'', 2),
    (5, CAST(1 AS bit), ''PRE-005'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000103'', NULL, NULL, N''Clínica Central'', 3),
    (6, CAST(1 AS bit), ''PRE-006'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000104'', NULL, NULL, N''Centro Médico del Valle'', 3),
    (7, CAST(1 AS bit), ''PRE-007'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000105'', NULL, NULL, N''Laboratorio Clínico Referencia'', 4),
    (8, CAST(1 AS bit), ''PRE-008'', ''2026-01-01T00:00:00.0000000+00:00'', N''seed'', ''130000106'', NULL, NULL, N''Laboratorio e Imágenes Diagnósticas'', 4)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Codigo', N'CreadoEn', N'CreadoPor', N'Documento', N'ModificadoEn', N'ModificadoPor', N'Nombre', N'TipoPrestadorId') AND [object_id] = OBJECT_ID(N'[Prestadores]'))
        SET IDENTITY_INSERT [Prestadores] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Afiliados_ContratanteId] ON [Afiliados] ([ContratanteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Afiliados_Documento] ON [Afiliados] ([Documento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Afiliados_IntermediarioId] ON [Afiliados] ([IntermediarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Afiliados_NumeroAfiliado] ON [Afiliados] ([NumeroAfiliado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Autorizaciones_AfiliadoId] ON [Autorizaciones] ([AfiliadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Autorizaciones_ClaveIdempotencia] ON [Autorizaciones] ([ClaveIdempotencia]) WHERE [ClaveIdempotencia] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Autorizaciones_ContratanteId] ON [Autorizaciones] ([ContratanteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Autorizaciones_FechaServicio] ON [Autorizaciones] ([FechaServicio]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Autorizaciones_IntermediarioId] ON [Autorizaciones] ([IntermediarioId]) WHERE [IntermediarioId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Autorizaciones_Numero] ON [Autorizaciones] ([Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Autorizaciones_PrestadorId] ON [Autorizaciones] ([PrestadorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_AutorizacionesDetalle_AutorizacionId] ON [AutorizacionesDetalle] ([AutorizacionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_AutorizacionesDetalle_TipoServicioId] ON [AutorizacionesDetalle] ([TipoServicioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contratantes_Codigo] ON [Contratantes] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contratantes_Documento] ON [Contratantes] ([Documento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Intermediarios_Codigo] ON [Intermediarios] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Prestadores_Codigo] ON [Prestadores] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Prestadores_Documento] ON [Prestadores] ([Documento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE INDEX [IX_Prestadores_TipoPrestadorId] ON [Prestadores] ([TipoPrestadorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TiposPrestadores_Codigo] ON [TiposPrestadores] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TiposServicios_Codigo] ON [TiposServicios] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123175817_InicialEsquemaYCatalogos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123175817_InicialEsquemaYCatalogos', N'10.0.15');
END;

COMMIT;
GO

