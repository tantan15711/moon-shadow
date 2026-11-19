# Desarrollo y exportación

## Entorno

- Unity Editor **6000.6.9f1** (`8c45277fb9c5`).
- Universal Render Pipeline **17.6.0**.
- Input System **1.20.1**.
- Android: módulos oficiales de Android Build Support, SDK, NDK y OpenJDK del editor.

El repositorio contiene el proyecto Unity completo: `Assets`, `Packages` y `ProjectSettings`. Unity crea `Library` al importarlo. Conserva los archivos `.meta` cuando añadas, muevas o renombres recursos; utiliza el editor para estas operaciones.

## Compilaciones de distribución

En PowerShell, desde la raíz del repositorio:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe'
$projectRoot = (Get-Location).Path
New-Item -ItemType Directory -Path '_Logs' -Force | Out-Null

& $unityEditor -batchmode -nographics -quit `
    -projectPath $projectRoot -buildTarget StandaloneWindows64 `
    -executeMethod MoonShadow.Editor.DistributionBuild.Windows `
    -logFile "$projectRoot\_Logs\build-windows.log"

& $unityEditor -batchmode -nographics -quit `
    -projectPath $projectRoot -buildTarget Android `
    -executeMethod MoonShadow.Editor.DistributionBuild.Android `
    -logFile "$projectRoot\_Logs\build-android.log"
```

Ejecuta las dos compilaciones de forma secuencial. Cierra el editor interactivo antes de usar el mismo proyecto en batchmode. La activación del editor debe estar resuelta en Unity Hub.

| Plataforma | Salida |
| --- | --- |
| Windows | `Builds/Windows/Moon Shadow.exe` y sus archivos de datos |
| Android | `Builds/Android/Moon Shadow.apk` |

Cada compilación correcta escribe `MOON_DISTRIBUTION_READY` en su registro. Para distribuir Windows, comprime el ejecutable junto con todos los archivos y carpetas de su salida, excluyendo la carpeta `BackUpThisFolder_ButDontShipItWithYourGame`.

El exportador Android configura IL2CPP, ARM64 y ARMv7, API mínima 26, API objetivo 36, OpenGL ES 3 y texturas ETC2. Usa la firma de desarrollo del entorno. La firma de publicación y la preparación de una ficha de tienda son tareas independientes.

## Pruebas de regresión

`Tools/Run-QA.ps1` acepta tres modos:

| Modo | Alcance |
| --- | --- |
| `Campaign` | Campaña completa, menús, combate, armas, rescates, progresión y guardados |
| `TouchReview` | Eventos multitáctiles, disparo mientras se mueve, recarga, salto, pausa e inventario |
| `MotionReview` | Revisión visual del movimiento y poses de Kyle y enemigos |

```powershell
.\Tools\Run-QA.ps1 -Mode Campaign -Editor $unityEditor
.\Tools\Run-QA.ps1 -Mode TouchReview -Editor $unityEditor
.\Tools\Run-QA.ps1 -Mode MotionReview -Editor $unityEditor -TimeoutSeconds 300
```

El script copia temporalmente los archivos de `Tools/QA` a las carpetas de scripts de Unity, construye un ejecutable de prueba y comprueba tanto su código de salida como el marcador de éxito. Al terminar retira únicamente los archivos temporales que creó. Si detecta archivos de QA previos, se detiene para no sobrescribirlos. No ejecutes simultáneamente dos instancias del script en el mismo checkout.

También puedes establecer `UNITY_EDITOR` con la ruta del editor y omitir `-Editor`. El runner localiza el proyecto a partir de su propia carpeta, por lo que el clon puede estar en cualquier ruta.

## Rendimiento

- Proyectiles del jugador y partículas reutilizan objetos para reducir asignaciones.
- Las mallas de personajes fuera de cámara se actualizan con menos frecuencia.
- Android utiliza menos partículas ambientales, limita la resolución y reduce el coste de renderizado.
- Las texturas Android tienen overrides ETC2; las imágenes de gran formato y el resto del arte usan límites específicos.
- El framerate objetivo es 60 en móvil y 120 en PC; la sincronización vertical de PC puede limitarlo según la pantalla.

Mide siempre la versión final en el dispositivo objetivo. Las cifras de QA de PC y la simulación de entrada táctil no sustituyen las mediciones en un teléfono.

## Historial Git

Los commits iniciales incorporan el estado actual del proyecto por alcance: juego y recursos, herramientas de QA y documentación. No representan fechas de desarrollo reconstruidas. Para cambios posteriores, utiliza mensajes concretos como `fix(combat): prevent duplicate projectile hits` o `feat(ui): add gamepad inventory navigation`.
