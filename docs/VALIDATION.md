# Validación de v0.9.2

Fecha de la entrega: **19 de noviembre de 2026**.

## Resultados registrados

| Comprobación | Resultado |
| --- | --- |
| Campaña automática de cinco actos | `MOON_CAMPAIGN_PASSED` |
| Entrada táctil con dos dedos y acciones de interfaz | `MOON_TOUCH_REVIEW_PASSED` |
| Arranque del ejecutable final de Windows | Correcto; se mantuvo activo durante la comprobación |
| Compilación de Windows de 64 bits | `MOON_DISTRIBUTION_READY` |
| Compilación Android IL2CPP | `MOON_DISTRIBUTION_READY` |
| Firma APK | Esquema v2 verificado; certificado Android Debug |
| Arquitecturas incluidas en el APK | `arm64-v8a`, `armeabi-v7a` |
| Instalación en MSI App Player / BlueStacks Pie64 | Correcta, versión 0.9.2 instalada |
| Integridad del ZIP de Windows | Cada archivo comprimido contrastado con su salida de compilación mediante SHA-256 |

Los checks de campaña incluyen navegación con teclado y Xbox, progresión, saltos y aterrizajes, arenas, jefes, rescates, guardados y continuación, puntería diagonal y disparo continuo con armas legendarias. Las pruebas táctiles introducen eventos de `Touchscreen` en el ejecutable de QA; no son una medición del rendimiento de Android.

## Rendimiento de referencia de PC

| Métrica | Resultado |
| --- | --- |
| GPU | NVIDIA GeForce RTX 4050 Laptop GPU |
| Resolución | 1280 × 720 |
| Muestras durante la campaña | 7366 |
| FPS medios | 115.6 |
| Tiempo medio por fotograma | 8.65 ms |
| Percentil 95 | 8.50 ms |
| Máximo registrado de memoria asignada por Unity | 234.8 MiB |

La prueba limita el juego a 120 FPS y excluye cargas, estados sin juego y fotogramas superiores a 150 ms. Estas cifras describen esa ejecución y ese equipo; no equivalen a una garantía de rendimiento en todos los dispositivos ni a un perfil completo de tirones durante las cargas.

## Validación pendiente

- Rendimiento, temperatura y consumo sostenido en teléfonos Android físicos.
- Ajuste de legibilidad y comodidad de los controles táctiles en distintos tamaños de pantalla.
- Preparación de firma de publicación y entrega a tiendas, si se decide publicarlo.

Los registros de ejecución y las cachés locales se excluyen del repositorio. Las pruebas pueden repetirse siguiendo [DEVELOPMENT.md](DEVELOPMENT.md).
