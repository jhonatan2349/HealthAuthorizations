# Base de datos

| Archivo | Descripción |
|---------|-------------|
| `migrations.sql` | Script **idempotente** generado desde las migraciones de EF Core (esquema + datos iniciales de catálogos). Puede ejecutarse varias veces: solo aplica las migraciones que falten. |

## Aplicar el esquema

**Con EF Core (desarrollo):**
```bash
dotnet tool restore
dotnet ef database update -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api
```

**Con el migrador en contenedor (compose / Kubernetes):**
```bash
docker compose up migrator          # o: docker build --target migrator -t healthauthorizations-migrator .
```
Es un bundle de EF Core autocontenido: aplica solo las migraciones pendientes y termina. Lee la cadena de
`ConnectionStrings__Default` (usuario con permisos DDL). La API no migra al arrancar.

**Con el script (DBA / pipeline):**
```bash
sqlcmd -S <servidor> -d HealthAuthorizations -I -i database/migrations.sql
```
> `-I` activa `QUOTED_IDENTIFIER`, requerido por la columna calculada persistida y los índices filtrados.

## Regenerar el script tras una nueva migración
```bash
dotnet ef migrations add <Nombre> -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api -o Persistence/Migrations
dotnet ef migrations script --idempotent -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api -o database/migrations.sql
```
