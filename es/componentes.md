# Referencia de componentes

Todo bloque de acá está armado con las ocho compuertas primitivas y con otros bloques. El bit 0 de
cualquier bus es el menos significativo.

## Compuertas — `Fanout.Gates`

| Tipo | Entradas | Comportamiento |
|---|---|---|
| `And` | 2 por defecto, cualquier cantidad | Alta cuando todas las entradas están altas |
| `Nand` | 2 por defecto, cualquier cantidad | Baja cuando todas las entradas están altas |
| `Or` | 2 por defecto, cualquier cantidad | Alta cuando alguna entrada está alta |
| `Nor` | 2 por defecto, cualquier cantidad | Baja cuando alguna entrada está alta |
| `Xor` | 2 por defecto, cualquier cantidad | Alta con paridad impar |
| `Xnor` | 2 por defecto, cualquier cantidad | Alta con paridad par |
| `Not` | 1 | Invierte |
| `BufferGate` | 1 | Deja pasar |

Cualquier compuerta puede tomar entradas negadas sin un inversor aparte:

```csharp
var compuerta = new And(PinPolarity.Inverted, PinPolarity.Normal);   // (¬a) ∧ b
```

## Combinacional — `Fanout.Combinational`

### `FullAdder`

| | Señales |
|---|---|
| Entradas | `A`, `B`, `CIN` |
| Salidas | `SUM`, `COUT` |

Dos XOR, dos AND, un OR.

### `RippleCarryAdder(int width = 2)`

| | Señales |
|---|---|
| Entradas | `A` (width), `B` (width), `CIN` |
| Salidas | `SUM` (width), `COUT` |

Una cadena de celdas `FullAdder`: el bit más alto no puede estabilizarse hasta que el acarreo
recorrió toda la cadena. Ojo: `Circuit.LastRunRounds` **no** mide esa profundidad — ver
[Cómo funciona](arquitectura.md#las-rondas-no-son-profundidad).

### `AddSubtractor(int width = 2)`

| | Señales |
|---|---|
| Entradas | `A` (width), `B` (width), `SUB` |
| Salidas | `SUM` (width), `COUT` |

Con `SUB` en bajo suma; con `SUB` en alto resta. Un XOR por bit invierte `B` y la misma señal
alimenta el acarreo de entrada, así que el sumador ve el complemento a dos de `B`. En una resta,
`COUT` es la bandera de *sin préstamo*: alta cuando `A ≥ B`.

### `Multiplexer(int inputCount = 2)`

| | Señales |
|---|---|
| Entradas | `SEL` (techo de log₂ n), `IN` (n) |
| Salidas | `OUT` |

Un AND por entrada de datos decodificando las líneas de selección, hacia un OR ancho.
`inputCount` tiene que ser al menos dos; no hace falta que sea potencia de dos, aunque las
entradas inalcanzables de un multiplexor que no lo sea simplemente nunca se seleccionan.

### `Demultiplexer(int outputCount = 2)`

| | Señales |
|---|---|
| Entradas | `SEL` (techo de log₂ n), `EN` |
| Salidas | `OUT` (n) |

`EN` se encamina hacia la salida direccionada; todas las demás quedan en bajo. Con `EN` fijo en
alto es un decodificador de direcciones.

## Secuencial — `Fanout.Sequential`

> **Leer esto primero.** Todo lo de esta sección está armado sobre un par de compuertas cruzadas,
> que no tiene estado definido hasta que algo lo fuerza. Un bloque con línea `CLR` necesita un
> pulso en bajo antes de hacer nada; uno sin ella se siembra a sí mismo en el constructor. Si una
> salida secuencial lee `Unknown`, casi siempre es porque falta el pulso de borrado.

### `NorSRLatch`

| | Señales |
|---|---|
| Entradas | `R`, `S` (las dos activas por alto) |
| Salidas | `Q`, `QN` |

`S` en alto pone, `R` en alto borra, las dos en bajo mantiene. Las dos en alto es el estado
prohibido y deja las dos salidas en bajo.

### `NandSRLatch`

| | Señales |
|---|---|
| Entradas | `SN`, `RN` (las dos activas por **bajo**) |
| Salidas | `Q`, `QN` |

`SN` en bajo pone, `RN` en bajo borra, las dos en alto mantiene. Las dos en bajo es el estado
prohibido y deja las dos salidas en alto. El código original llamaba a estos pines `R` y `S`, al
revés — un latch NAND es activo por bajo, así que el pin que llamaba `R` es el que pone.

### `DLatch`

| | Señales |
|---|---|
| Entradas | `D`, `CLK` |
| Salidas | `Q`, `QN` |

Cuatro NAND. Sensible a nivel: transparente mientras `CLK` está en alto, mantiene mientras está en
bajo.

### `DLatchPrimitive`

Es un `Gate`, no un `Module`. Entradas por índice: `0` = D, `1` = habilitación. Salidas: `Output`
(Q) y `OutputQN`. Es el ejemplo trabajado de escribir el comportamiento en código en vez de
cablearlo con compuertas — conviene compararlo con `DLatch`, que es el mismo elemento por el camino
largo.

### `DFlipFlop(ClockEdge edge = Falling, bool settlePresetAndClear = true)`

| | Señales |
|---|---|
| Entradas | `D`, `CLK`, `PRE`, `CLR` (las dos activas por bajo) |
| Salidas | `Q`, `QN` |

Maestro-esclavo, disparado por flanco. `PRE` y `CLR` actúan sin importar el reloj. Con
`settlePresetAndClear` en su valor por defecto arrancan inactivas, así que un circuito que nunca
las conecte igual estabiliza — pero el flip-flop sigue necesitando un pulso de `CLR` para salir del
estado indeterminado.

### `SimpleDFlipFlop(ClockEdge edge = Falling)`

`D`, `CLK` → `Q`, `QN`. Lo mismo sin preset ni borrado, así que nada puede forzar un estado
inicial; `Q` queda indeterminada hasta que el primer flanco resuelve el par.

### `TFlipFlop(ClockEdge edge = Falling, bool settlePresetAndClear = true)`

| | Señales |
|---|---|
| Entradas | `T`, `PRE`, `CLR` |
| Salidas | `Q`, `QN` |

`T` es a la vez la entrada de conmutación y el reloj: cada flanco seleccionado sobre `T` da vuelta
la salida. Eso es lo que hace que una cadena de estos sea un contador.

### `SimpleTFlipFlop(ClockEdge edge = Falling)`

`T` → `Q`, `QN`. Sin un borrado que fuerce un estado, el constructor siembra el par de salidas
directamente en `Q = 0`. Es un atajo de tiempo de construcción sin contraparte física — un
flip-flop real arranca en el estado en que caiga.

### `JKFlipFlop(ClockEdge edge = Falling, bool settlePresetAndClear = true)`

| | Señales |
|---|---|
| Entradas | `J`, `K`, `CLK`, `PRE`, `CLR` |
| Salidas | `Q`, `QN` |

En el flanco seleccionado: `00` mantiene, `10` pone, `01` borra, `11` conmuta.

### `SimpleJKFlipFlop(ClockEdge edge = Falling)`

`J`, `K`, `CLK` → `Q`, `QN`. Se siembra en `Q = 0` igual que `SimpleTFlipFlop`.

### `ShiftRegister(int width = 2, ClockEdge edge = Rising)`

| | Señales |
|---|---|
| Entradas | `D` (serie), `CLK`, `PRE`, `CLR` |
| Salidas | `Q` (width) |

Entrada serie, salida paralelo. `Q[0]` es la etapa más cercana a la entrada.

### `ParallelRegister(int width = 2, ClockEdge edge = Rising)`

| | Señales |
|---|---|
| Entradas | `D` (width), `CLK`, `PRE`, `CLR` |
| Salidas | `Q` (width) |

Un flip-flop por bit, todos relojeados juntos.

### `ShiftLoadRegister(int width = 2, ClockEdge edge = Rising)`

| | Señales |
|---|---|
| Entradas | `D` (width), `SIN`, `LOAD`, `CLK`, `PRE`, `CLR` |
| Salidas | `Q` (width) |

Con `LOAD` en alto toma las entradas paralelas en el próximo flanco; con `LOAD` en bajo desplaza,
metiendo `SIN` por el bit 0.

### `RippleCounter(int width = 2, ClockEdge edge = Rising, CountDirection direction = Up)`

| | Señales |
|---|---|
| Entradas | `CLK`, `PRE`, `CLR` |
| Salidas | `Q` (width) |

Una cadena de `TFlipFlop`, cada uno relojeando al siguiente. Cuál salida alimenta a la etapa
siguiente no es una preferencia: una etapa tiene que entregarle a su vecina una transición de
disparo exactamente cuando vuelve a cero, así que contar para arriba con flanco de subida implica
encadenar `QN`, y contar para arriba con flanco de bajada implica encadenar `Q`. `direction` y
`edge` lo deciden entre las dos y quien llama nunca tiene que saber la regla.

Como es asincrónico, en un circuito real las etapas no cambian juntas — el bit más alto se
estabiliza recién después de que el acarreo recorrió la cadena. `Circuit.Run` siempre informa el
valor estabilizado, así que esos transitorios no se ven acá.
