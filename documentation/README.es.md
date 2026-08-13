<div align="center">

![Banner de Freeze Ray](./banner.png)

**Congela una ventana: mantenla visible en el mismo sitio en todos los escritorios virtuales, y por encima de todo lo demás.**

[English](../README.md) · [Français](./README.fr.md) · [Deutsch](./README.de.md) · **Español** · [Italiano](./README.it.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [Русский](./README.ru.md) · [中文](./README.zh.md)

<p align="center">
  <a href="https://github.com/Game-K-Hack/freeze-ray/releases/latest"><img src="https://img.shields.io/github/v/release/Game-K-Hack/freeze-ray?label=Descargar&style=for-the-badge&logo=windows" alt="Descargar"></a>
</p>

</div>

## Para qué sirve

Windows permite cambiar de escritorio virtual con `Ctrl + Win + ←/→`, pero todo lo
que estabas mirando desaparece junto con el escritorio que dejas atrás. Freeze Ray
fija una ventana para que **siga visible, exactamente en el mismo sitio, en todos
los escritorios**.

Es una utilidad del área de notificación en el espíritu de **DeskPin**, con dos
acciones independientes:

- **Mantener en pantalla**: la ventana te acompaña por todos los escritorios
  virtuales.
- **Siempre visible**: la ventana permanece por encima de las demás, el
  comportamiento clásico de DeskPin.

Ambas pueden aplicarse a la misma ventana.

## Requisitos

- **Windows 10** (compilado y verificado en la compilación 19045, 22H2).
- **.NET Framework 4**: ya viene con Windows, no hay nada que instalar.

> En Windows 11, las interfaces no documentadas del shell usadas para los
> escritorios virtuales tienen otros identificadores. Véase
> [Limitaciones conocidas](#limitaciones-conocidas).

## Primeros pasos

1. Descarga `Freeze Ray.exe` desde la
   [última versión](https://github.com/Game-K-Hack/freeze-ray/releases/latest),
   o [compílalo tú mismo](#compilar-desde-el-código-fuente).
2. Ejecútalo. No se abre ninguna ventana: solo aparece un icono en el área de
   notificación.
3. Haz clic en el icono → **Mantener en pantalla (todos los escritorios)…**
4. El cursor se convierte en el logotipo de la aplicación: haz clic en la ventana
   que quieras conservar. Recibirá un pequeño logotipo en su barra de título.
5. Cambia de escritorio con `Ctrl + Win + ←/→`: la ventana sigue ahí.
6. Para liberarla, haz clic en el logotipo de su barra de título.

El ejecutable es autónomo: no necesita instalador ni la carpeta `assets`.

## Uso

**No se registra ningún atajo de teclado global**, de modo que nada puede chocar
con otra aplicación. Todo pasa por el icono, y **un clic —izquierdo o derecho—
abre el menú**:

| Entrada | Efecto |
|---|---|
| **Mantener en pantalla (todos los escritorios)…** | Entra en modo selección; la ventana en la que hagas clic seguirá todos los escritorios |
| **Siempre visible…** | Entra en modo selección; la ventana en la que hagas clic pasa a `TOPMOST` |
| **Ventanas bloqueadas (n)** | Las lista con su estado; al hacer clic en una, se libera |
| **Liberar todo** | Devuelve todas las ventanas a su estado normal |
| **Configuración…** | Abre la ventana de ajustes |
| **Salir** | |

### El modo selección

Tras hacer clic en una de las dos primeras entradas, **el cursor se convierte en
el logotipo**, señalando que se espera una ventana; el siguiente clic la elige.
Freeze Ray consume ese clic, así que no acciona lo que haya bajo el puntero.

- **Esc** o un **clic derecho** cancelan. Hacer clic en el escritorio o en la barra
  de tareas también desiste, sin mensaje.
- Seleccionar una ventana ya bloqueada la libera: la acción alterna.
- La información sobre herramientas del icono muestra siempre el estado actual.

### La marca en la barra de título

Una ventana bloqueada recibe **el logotipo en su barra de título**, justo a la
izquierda de los botones del sistema. **Al hacer clic en él, la ventana se libera**
y la marca desaparece.

La marca sigue a su ventana al moverla o redimensionarla, desaparece al
minimizarla y se sitúa justo delante de ella en el orden de profundidad: otra
ventana que tape el objetivo tapa también la marca. Una ventana mantenida en todos
los escritorios se lleva su marca de uno a otro.

Las ventanas con marco personalizado (navegadores, aplicaciones UWP…) no siempre
publican la geometría de su barra de título; entonces la marca se coloca en la
esquina superior derecha del marco visible.

## Configuración

Accesible mediante **Configuración…** en el menú. La ventana muestra el logotipo,
el nombre y el **número de versión**, y luego:

| Ajuste | Detalle |
|---|---|
| **Iniciar con Windows** | Escribe en `HKCU\...\CurrentVersion\Run`. El registro sigue siendo la única fuente de verdad: la casilla vuelve a leer el estado real y se realinea si la escritura falla |
| **Liberar todo al salir** | Evita dejar ventanas bloqueadas |
| **Mostrar notificaciones** | Solo oculta los globos informativos: **los errores siempre se avisan**, porque silenciarlos haría que una acción fallida pareciera una acción sin efecto |
| **Idioma** | Se aplica de inmediato, incluidos menú, información sobre herramientas y notificaciones |
| **Repositorio de GitHub** | Fuente usada para buscar actualizaciones, con el formato `propietario/repositorio` |

Los ajustes viven en `%APPDATA%\Freeze Ray\settings.ini`, un simple archivo
`clave=valor` que puedes leer y corregir a mano. En el primer arranque el idioma sigue al de Windows, con reserva en inglés. Hay nueve idiomas disponibles: inglés, francés, alemán, español, italiano, japonés, coreano, ruso y chino.

Los textos están en [Strings.cs](../Strings.cs), una tabla por idioma en lugar de
archivos de recursos, para que el proyecto siga compilándose con el compilador que
trae Windows. Añadir un idioma consiste en añadir una tabla y una entrada en la
lista desplegable.

### Actualizaciones

**Buscar actualizaciones** consulta la API pública de versiones de GitHub para el
repositorio configurado, compara los números y ofrece abrir la página de descarga.

**La aplicación no se actualiza sola, y es deliberado.** Reemplazar un ejecutable
en marcha exige un proceso auxiliar, y hacerlo sin firma ni verificación de
integridad sería un vector de ataque: no compensa para una utilidad de este
tamaño.

## Compilar desde el código fuente

No hace falta ningún SDK: basta el compilador de C# que acompaña a .NET Framework
4, ya presente en Windows.

```bat
build.bat
```

Esto produce `Freeze Ray.exe` junto a las fuentes. El logotipo va **incrustado en
el ejecutable**, así que el binario funciona por sí solo.

## Cambiar el logotipo

| Archivo | Función |
|---|---|
| `assets/icon.png` | Logotipo de origen (512×512, transparente): icono del área de notificación, cursor de selección y marca de la barra de título |
| `assets/app.ico` | **Generado** por `tools/MakeIcon.cs`: icono del archivo y de la ventana |
| `assets/Freeze Ray.png` | Ilustración usada solo en la cabecera de la configuración |

`icon.ico` contenía originalmente una única imagen de 256×256 que Windows habría
tenido que reducir por su cuenta para el área de notificación (16×16) y la barra
de título, con un resultado borroso. Por eso `tools/MakeIcon.cs` precalcula los
nueve tamaños útiles (16 → 256) a partir del PNG con un remuestreo de calidad.

Para cambiar el logotipo, sustituye `assets/icon.png` y regenera:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:MakeIcon.exe /r:System.Drawing.dll tools\MakeIcon.cs
MakeIcon.exe
build.bat
```

## Cómo funciona

### Escritorios virtuales

Mantener una ventana en todos los escritorios usa el mismo mecanismo que la opción
*«Mostrar esta ventana en todos los escritorios»* del menú contextual de la Vista
de tareas, expuesto por las interfaces COM no documentadas del shell
`IApplicationViewCollection` e `IVirtualDesktopPinnedApps`; véase
[VirtualDesktop.cs](../VirtualDesktop.cs).

### Selección con una capa, no con captura del ratón

La selección se apoya en una **capa transparente que cubre todos los monitores**,
no en `SetCapture`. La captura del ratón solo redirige mensajes mientras se
mantiene pulsado un botón o mientras el puntero está sobre la ventana que captura;
por eso la herramienta de búsqueda de Spy++ se usa *arrastrando*. Sin botón
pulsado, cada ventana sobrevolada seguía imponiendo su propio cursor y el logotipo
no aparecía nunca. Con la capa, el puntero está permanentemente sobre nuestra
propia ventana: ella impone su cursor y recibe el clic. Véase
[WindowPicker.cs](../WindowPicker.cs).

### La marca

La marca es una ventana con transparencia por píxel (`WS_EX_LAYERED` +
`UpdateLayeredWindow`), lo que conserva el suavizado del logotipo sobre cualquier
fondo. Nunca toma el foco, así que hacer clic en ella no desactiva la ventana
objetivo, y sus zonas transparentes dejan pasar el clic hacia la barra de título
que hay debajo.

**Para desplazar la marca**, un único ajuste en
[WindowMarker.cs](../WindowMarker.cs): `BUTTON_GAP`, la separación respecto al
primer botón del sistema (4 px). Cuanto menor sea, más a la derecha queda la
marca; por debajo de cero se solapa con el botón Minimizar.

El ancho del bloque de botones no puede leerse directamente: la métrica del
sistema `SM_CXSIZE` indica 36 px allí donde Windows 10 dibuja botones de 46 px
(medido al píxel: glifos centrados cada 46 px). En cambio sí sigue correctamente
la escala de pantalla, de ahí la proporción 46/36 usada en el código.

### Aplicaciones que vetan el «siempre visible»

Algunas aplicaciones **se niegan** a que se cambie su orden de profundidad:
interceptan `WM_WINDOWPOSCHANGING` y neutralizan el cambio de paso. `SetWindowPos`
devuelve entonces **éxito sin haber hecho nada**: VLC se comporta así mientras
reproduce un vídeo (medido: la marca seguía ausente un segundo entero después de
la llamada).

De ahí dos precauciones en el código:

- el indicador `SWP_NOSENDCHANGING` suprime esa notificación y priva a la
  aplicación de su derecho de veto;
- el estado se **vuelve a leer después** en lugar de confiar en el valor devuelto,
  para que un fallo real se avise en vez de pasar en silencio.

### Notificaciones

Los globos informativos muestran **el logotipo de la aplicación** en lugar de la
«i» azul del sistema. WinForms no sabe hacerlo: `NotifyIcon.ShowBalloonTip` solo
acepta iconos del sistema y rechaza cualquier valor fuera de su enumeración. Por
eso se habla directamente con el shell (`Shell_NotifyIcon` con `NIIF_USER`),
reutilizando la identificación interna de la entrada que creó WinForms; véase
[Notifications.cs](../Notifications.cs). Si ese detalle interno cambiara algún día,
el código recae en el globo estándar.

La cabecera de la notificación muestra `Freeze Ray.exe`: Windows pone ahí el
nombre del archivo ejecutable. Declarar un `AppUserModelID` no cambia nada
(comprobado); solo instalar un acceso directo en el menú Inicio permitiría un
nombre sin extensión.

## Limitaciones conocidas

- Una ventana perteneciente a un proceso **con privilegios elevados** solo puede
  modificarse si Freeze Ray también se ejecuta como administrador.
- Las interfaces COM usadas para los escritorios virtuales no están documentadas y
  sus identificadores cambian entre versiones de Windows. Los GUID empleados aquí
  son los de **Windows 10 1803 → 22H2**, verificados en la compilación **19045**.
  En Windows 11, `IVirtualDesktopPinnedApps` tiene otro IID y habrá que ajustar
  [VirtualDesktop.cs](../VirtualDesktop.cs).
- La fijación se aplica a la ventana, no a la aplicación: si cierras una ventana y
  la vuelves a abrir, hay que fijarla de nuevo.
