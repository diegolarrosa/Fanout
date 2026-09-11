# Fanout

Un simulador de lógica digital a nivel de compuerta para .NET. Ocho compuertas primitivas, lógica
de tres estados y un lazo de propagación dirigido por eventos — y todo lo demás de la librería,
hasta los registros de desplazamiento y los contadores, está armado con esas compuertas y nada más.

*English documentation: [`../README.md`](../README.md).*

## Qué es

Fanout simula un circuito como el circuito realmente se comporta: cambia un valor en un cable, se
vuelven a evaluar las compuertas que ese cable alcanza, y lo que cambie como consecuencia se
propaga hacia adelante, hasta que ya no cambia nada. No hay reloj dentro del simulador, no hay
paso de tiempo y no hay calendario de eventos. Hay una cola de compuertas que tienen algo nuevo
para mirar.

Lo que hace que eso funcione es el tercer estado lógico. Todo cable arranca en **indeterminado**,
no en cero, y una compuerta se evalúa recién cuando tiene información suficiente para dar una
respuesta. "Suficiente" es menos que "toda", y ahí está lo interesante:

```
Una compuerta AND con una entrada ya en cero tiene la salida decidida.
No espera a las demás.
```

Esa sola regla —evaluar ante un valor controlante— es la razón de que un circuito pueda estabilizarse
con la mitad sin excitar, de que un latch de compuertas cruzadas se resuelva apenas se fuerza uno
de sus lados, y de que el simulador pueda decirte que un latch **todavía no tiene estado** en vez
de inventarte un cero.

## Instalación

```bash
dotnet add package Fanout
```

## Un primer circuito

```csharp
using Fanout;
using Fanout.Combinational;

var circuito = new Circuit();
var sumador = new RippleCarryAdder(width: 4);

// Puertos del nivel superior, conectados a las entradas con nombre del sumador.
var a = new Port[4];
var b = new Port[4];

for (int i = 0; i < 4; i++)
{
    a[i] = circuito.AddInput();
    a[i].ConnectTo(sumador, "A", i);

    b[i] = circuito.AddInput();
    b[i].ConnectTo(sumador, "B", i);
}

var acarreoEntrada = circuito.AddInput();
acarreoEntrada.ConnectTo(sumador, "CIN");

// Se excita con 9 + 6 + 0 y se estabiliza.
Excita(a, 9);
Excita(b, 6);
acarreoEntrada.SetState(LogicState.Zero);

int rondas = circuito.Run();

Console.WriteLine(sumador.Output("SUM", 3).State);   // One
Console.WriteLine(sumador.Output("COUT").State);     // Zero

static void Excita(Port[] bus, int valor)
{
    for (int i = 0; i < bus.Length; i++)
    {
        bus[i].SetState(((valor >> i) & 1) == 0 ? LogicState.Zero : LogicState.One);
    }
}
```

`dotnet run --project samples/Fanout.Demo` corre una versión más larga: un sumador, un contador,
un registro de desplazamiento y una tabla de cómo crece la profundidad de propagación con la
cadena de acarreo.

## Qué trae

| Espacio de nombres | Contenido |
|---|---|
| `Fanout` | `LogicState`, `Gate`, `InputPin`, `OutputPin`, `Net`, `Port`, `Module`, `Circuit`, `ModuleLayout` |
| `Fanout.Gates` | `And`, `Or`, `Not`, `Nand`, `Nor`, `Xor`, `Xnor`, `BufferGate` |
| `Fanout.Combinational` | `FullAdder`, `RippleCarryAdder`, `AddSubtractor`, `Multiplexer`, `Demultiplexer` |
| `Fanout.Sequential` | `NandSRLatch`, `NorSRLatch`, `DLatch`, `DLatchPrimitive`, `DFlipFlop`, `SimpleDFlipFlop`, `TFlipFlop`, `SimpleTFlipFlop`, `JKFlipFlop`, `SimpleJKFlipFlop`, `ShiftRegister`, `ParallelRegister`, `ShiftLoadRegister`, `RippleCounter` |

Todo bloque por encima del nivel de compuerta está armado con compuertas y con otros bloques — el
flip-flop JK son ocho NAND, un AND y dos inversores, y el contador es una cadena de esos. Nada se
atajó con código de comportamiento, salvo una excepción deliberada (`DLatchPrimitive`), que queda
como el ejemplo trabajado de extender `Gate` directamente.

> **Nota sobre el idioma.** El código y la API están en inglés a propósito, para que la librería
> sirva fuera del mundo hispanohablante. Esta carpeta `es/` es la documentación completa en
> castellano; los nombres de tipos y señales que aparecen acá son los mismos que en el código.

## Documentación

- [Primeros pasos](primeros-pasos.md) — puertos, excitar entradas, leer resultados
- [Cómo funciona](arquitectura.md) — el lazo de propagación, los valores controlantes, por qué termina
- [Referencia de componentes](componentes.md) — cada bloque, sus señales y sus mañas
- [Escala y memoria](escalado.md) — cuánto cuesta este diseño por compuerta, y dónde está el techo
- [Traspaso](TRASPASO.md) — estado del port: qué quedó cerrado y qué queda abierto

## Para qué sirve, y para qué no

Sirve para entender y para armar circuitos sobre los que se pueda razonar: material didáctico, una
implementación de referencia, una forma de comprobar que un diseño hace lo que uno dibujó. Los
bloques son lo bastante chicos como para leerlos de punta a punta, y la regla de propagación entra
en un párrafo.

No es una herramienta de EDA de producción. No tiene modelo temporal, así que los glitches y los
riesgos de un circuito real son invisibles acá — `Run` informa el resultado estabilizado y nada
más. No tiene resolución de múltiples excitadores, ni estado de alta impedancia, ni importación de
netlists. Y guarda cada compuerta como un grafo de objetos .NET, lo que pone un techo práctico al
tamaño del circuito bastante por debajo de lo que alcanzaría una representación empaquetada.
[escalado.md](escalado.md) saca esa cuenta de frente en vez de dejarla implícita.

## Procedencia

El original se escribió en 2005 contra .NET 2.0 y Visual Studio 2005, y quedó sin publicar durante
veinte años. Esta versión lo porta a .NET 8, lo traduce al inglés, reemplaza las colecciones no
genéricas, corrige los defectos listados en [CHANGELOG.md](../CHANGELOG.md) y agrega la suite de
pruebas que nunca tuvo. El diseño de propagación —tres estados, evaluación por valor controlante,
la cola de trabajo de doble buffer— quedó igual, porque estaba bien de entrada.

## Licencia

MIT. Ver [LICENSE](../LICENSE).
