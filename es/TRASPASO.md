# Traspaso — estado del port

Este documento es lo primero que hay que leer al retomar el proyecto. Dice qué quedó cerrado, qué
queda abierto y qué está y qué no está verificado.

## Estado

| Ítem | Estado |
|---|---|
| Port a .NET 8, proyectos SDK-style | ✅ **Hecho** |
| Traducción completa al inglés (tipos, miembros, comentarios, nombres de señal) | ✅ **Hecho** — tabla de renombrado en `CHANGELOG.md` |
| Fuentes normalizadas a UTF-8 | ✅ **Hecho** — el original mezclaba Windows-1252 y UTF-16LE |
| `GateQueue` es dueño de su almacenamiento (el viejo defecto del `Hashtable` nulo) | ✅ **Corregido** |
| `Array.Resize` cuadrático en cada entrada agregada | ✅ **Corregido** — las entradas viven en una lista |
| La salida de cada compuerta se crea en el constructor base, nunca es nula | ✅ **Corregido** |
| El contador encadena la salida correcta según su flanco y su dirección | ✅ **Corregido** — cambio de comportamiento respecto del original |
| Los pines del latch NAND se llaman como lo que hacen (`SN`/`RN`) | ✅ **Corregido** |
| Guarda contra oscilación en vez de lazo infinito | ✅ **Agregado** — `CircuitOscillationException` |
| `CustomModule` recibe su disposición en vez de un registro estático global | ✅ **Corregido** |
| Un nombre de señal desconocido lanza un mensaje que lista los válidos | ✅ **Agregado** |
| WinForms afuera, demo de consola en su lugar | ✅ **Hecho** |
| Suite de pruebas xUnit (el original no tenía ninguna) | ✅ **Escrita** — ver la salvedad de abajo |
| Documentación XML en cada miembro público | ✅ **Hecho** |
| Documentación en inglés (`README.md`, `docs/`) | ✅ **Hecha** |
| Documentación en castellano (`es/`) | ✅ **Hecha** |
| `LICENSE` MIT, `.gitignore`, `.editorconfig`, workflow de CI | ✅ **Hecho** |
| **Que `dotnet build` y `dotnet test` efectivamente corran** | ✅ **Verificado.** Compila limpio en .NET 8.0.25; **74 de 74 pruebas pasan**; el demo corre correctamente |
| Conteo de rondas presentado como métrica de profundidad | ✅ **Corregido.** Depende del orden y no es profundidad lógica; documentación, demo y test arreglados |
| Modo de evaluación sincrónica (ciclo delta), para que las rondas *sí* signifiquen profundidad | ⬜ **Idea abierta**, ~40 líneas, deliberadamente sin hacer |
| Confirmar que el ID `Fanout` está libre en nuget.org | ✅ **Libre y confirmado.** `packageid:fanout` no devuelve nada; los 17 resultados de búsqueda son paquetes de mensajería que solo mencionan fan-out |
| Repositorio de GitHub creado y subido | ⬜ **Abierto** |
| Un tutorial "armá tu propio procesador" | ⬜ **Idea**, sin empezar |
| Importación/exportación de netlists | ⬜ **Idea**, sin empezar |

## Cómo se verificó

El port se escribió sin un SDK de .NET disponible, así que primero se revisó de forma estática y
después se comprobó en una máquina real. El resultado:

```bash
dotnet restore
dotnet build -c Release      # limpio, advertencias como errores, sin faltantes de doc XML
dotnet test -c Release       # 74 pasan, 0 fallan, 0 omitidas
dotnet run --project samples/Fanout.Demo
```

De todo el port salió exactamente un error de compilación: `CS0419`, un `cref` ambiguo apuntando a
`Circuit.AddInput`, que tiene dos sobrecargas. Los comentarios de documentación que nombran un
miembro sobrecargado necesitan la firma — `<see cref="Circuit.AddInput(Port)"/>`. Si más adelante
se agrega una sobrecarga nueva, conviene revisar los comentarios que la mencionen.

El demo destapó un defecto de fondo que las pruebas no habían visto: el conteo de rondas se estaba
presentando como una medida de profundidad lógica, y no lo es. Ver
[arquitectura.md](arquitectura.md#las-rondas-no-son-profundidad); la afirmación quedó corregida en
el código, la documentación, el demo y la suite de pruebas.

### Dónde mirar primero si algo se rompe más adelante

En orden aproximado de probabilidad:

1. **Resolución de sobrecargas de `ConnectTo`.** Hay varias sobrecargas repartidas entre `Gate`,
   `Port` y `Module`, distinguidas por si el destino es una compuerta, un módulo, un índice o un
   nombre. Una llamada que se enganche con la equivocada probablemente compile igual y después se
   porte mal, así que una prueba de *componente* que falle apunta acá antes que a la lógica.
2. **Orden de puertos contra disposición.** El orden de las llamadas a `AddInput` y `AddOutput` en
   el constructor de un módulo define los índices de puerto, y `ModuleLayout` tiene que coincidir.
   Una discrepancia es muda en tiempo de compilación. Las pruebas que lo cazan son la del sumador
   exhaustivo y la del multiplexor.
3. **Comentarios de documentación y `TreatWarningsAsErrors`.** La librería compila con las
   advertencias como errores y con `GenerateDocumentationFile`, así que un miembro público sin
   comentario XML rompe la compilación. Cada caso se arregla con una línea.

Los proyectos de pruebas y de demo ponen `TreatWarningsAsErrors` en `false` a propósito, para que
una actualización de analizadores de xUnit no pueda romper la compilación. La librería lo mantiene
prendido.

## Antes de publicar

1. Verificar que `Fanout` esté libre en nuget.org — el ID es global y por orden de llegada. Si
   está tomado, aparece en `Directory.Build.props`, `src/Fanout/Fanout.csproj`, los espacios de
   nombres y la documentación.
2. Confirmar la línea de copyright en `LICENSE` y en `Directory.Build.props`.
3. Subir a GitHub como repositorio privado, esperar que CI dé verde, y recién ahí hacerlo público.
4. Publicar 0.1.0 en NuGet.

## Decisiones de diseño que no conviene volver a discutir

- **Esta librería sigue siendo un grafo de objetos.** `es/escalado.md` deja sentado el costo por
  compuerta y por qué una representación empaquetada es otro proyecto y no una refactorización de
  este.
- **Sin modelo temporal.** `Run` devuelve el estado estabilizado. Agregar retardos convierte
  "estabilizar" en "agendar", que es otro simulador con otra API.
- **Un excitador por entrada.** Sin contención de bus, sin alta impedancia.
- **Los bloques siguen siendo legibles.** Un constructor lleno de `ConnectTo` que se lee como un
  esquemático es la característica, no un detalle de implementación a optimizar.
