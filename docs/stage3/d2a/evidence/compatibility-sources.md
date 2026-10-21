# Fuentes de compatibilidad verificadas

Consulta web oficial durante D2-A:
- https://learn.microsoft.com/en-us/ef/core/providers/sql-server/ — proveedor compatible con SQL Server 2019 en adelante; compatibilidad explícita 150.
- https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.13 — versión EF10 seleccionada.
- https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.13 — paquete Microsoft firmado por editor reservado, target net10.0 sin dependencias. Pin para resolver NU1902/NU1903 detectados en restore inicial, sujeto a auditoría final.

Estas fuentes justifican compatibilidad/configuración; no sustituyen pruebas sobre la instancia real ni certifican el runtime instalado.
