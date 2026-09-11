# Cómo funciona

El motor son unas trescientas líneas. Esta página es qué hacen esas trescientas líneas y por qué
están ordenadas así.

## Los tres estados

```csharp
public enum LogicState { Zero, One, Unknown }
```

`Unknown` no es un valor de error ni un cuarto nivel lógico. Es el registro que lleva el simulador
de *cuánto sabe hasta ahora*, y es lo que hace funcionar todo el esquema de propagación. Una
compuerta con entradas indeterminadas no tiene nada que calcular, así que se saltea. Cuando más
tarde cambia alguna de sus entradas, vuelve.

Sin un tercer estado haría falta una pasada de inicialización que decida en qué arranca cada
cable, y esa decisión es una mentira para cualquier cosa que tenga realimentación.

## El lazo de propagación

```
excitar una entrada  →  el valor difiere  →  encolar las compuertas que alcanza
                                                   ↓
                        ┌───── evaluar cada compuerta encolada ─────┐
                        │  la salida difiere → encolar lo que llega │
                        └──────────────── repetir ──────────────────┘
                                                   ↓
                                    nada encolado: estabilizado
```

En código, eso es todo `Circuit.Run`:

```csharp
while (current.Count > 0)
{
    next.Clear();

    foreach (Gate gate in current)
    {
        gate.TryEvaluate(next);
    }

    (current, next) = (next, current);
}
```

Dos cosas de esto son estructurales.

**La cola es un conjunto, no una lista.** Una compuerta de tres entradas a la que le cambian las
tres en la misma ronda aparece una vez, así que se evalúa una vez. Las compuertas se comparan por
referencia, que es la noción correcta de identidad para un elemento de circuito.

**Es de doble buffer, no una lista de trabajo.** Todo lo encolado en la ronda *n* se evalúa antes
que cualquier cosa encolada en la ronda *n+1*. Eso es *agendado* en anchura, y vale más de lo que
parece: significa que todos los elementos que están a la misma profundidad lógica se vuelven a
examinar juntos. Cuando una línea de reloj se abre hacia ocho flip-flops, los ocho ven el flanco
en la misma ronda, así que todos los latches maestros se cierran antes de que se abra cualquier
esclavo y ninguna etapa puede actuar sobre la salida nueva de su vecina. Una lista de trabajo en
profundidad haría que el registro de desplazamiento de esta librería tuviera carrera.

## Las rondas no son profundidad

Lo que está en doble buffer es la *cola*, no los *valores*. Una compuerta escribe su salida al
instante, así que una compuerta evaluada más tarde en la misma ronda ya la ve. Dentro de una ronda
el orden de evaluación no está especificado —es el orden del hash sobre un conjunto—, lo que
significa:

- **Los resultados no dependen del orden.** La propagación converge a un punto fijo y, para una
  red combinacional, ese punto fijo es único. El resultado es el resultado.
- **`Circuit.LastRunRounds` sí depende del orden.** Un orden favorable deja que una cadena de
  acarreo de 32 bits se estabilice en tres rondas; uno desfavorable tarda sesenta. El número es una
  señal aproximada de costo y nada más.

Si uno quiere que el conteo de rondas signifique profundidad lógica, la evaluación tiene que sacar
una foto de los valores al principio de cada ronda y confirmarlos al final — una evaluación
sincrónica, o de *ciclo delta*. Es un modo real y que vale la pena, y no es lo que hace este motor
hoy.

## Por qué termina

Porque `SetState` está guardado:

```csharp
if (State == value)
{
    return;
}
```

Una compuerta se encola solo cuando un valor **cambió de verdad**. Un circuito con una cantidad
finita de cables solo puede cambiar una cantidad finita de veces antes de quedarse sin cambios que
hacer — salvo que tenga un lazo de realimentación con una cantidad impar de inversiones, en cuyo
caso genuinamente nunca se estabiliza y la respuesta correcta es decirlo. `Circuit.MaxRounds` acota
el lazo y `CircuitOscillationException` lo informa.

Ojo con lo que **no** hay acá: ni límite de iteraciones por compuerta, ni heurística de
convergencia, ni amortiguación. El lazo es exacto.

## Valores controlantes

La regla por defecto es que una compuerta espera a todas sus entradas. Varias pueden hacerlo
mejor:

| Compuerta | Valor controlante | Entonces la salida es |
|---|---|---|
| `And` | `Zero` | `Zero` |
| `Nand` | `Zero` | `One` |
| `Or` | `One` | `One` |
| `Nor` | `One` | `Zero` |
| `Xor`, `Xnor` | ninguno | — |
| `Not`, `BufferGate` | — (una entrada) | — |

Esas cuatro sobrescriben `HasUnknownInputs` para resolverse apenas ven un valor controlante, sin
esperar al resto. Es propagación de restricciones común y corriente, y está haciendo trabajo real:

- Un latch NAND de compuertas cruzadas se resuelve en el instante en que se baja uno de sus lados,
  que es exactamente cómo se comporta el circuito físico. Sin esto, ninguna de las dos compuertas
  podría resolverse nunca y el latch quedaría indeterminado para siempre.
- Un borrado asincrónico llega a la salida por una cadena de compuertas que están todas en un
  valor controlante, así que surte efecto sin importar qué estén haciendo el reloj y el dato — que
  es justamente lo que significa "asincrónico".
- Un circuito grande con la mayoría de sus entradas sin excitar igual estabiliza la parte que sí
  está determinada, en vez de trabarse.

`AllInputsKnown` cachea el resultado una vez que se vio que todas las entradas están determinadas,
así que una compuerta ya estabilizada no vuelve a recorrer sus entradas en rondas posteriores.

## El grafo de objetos

```
Gate ──tiene──▶ OutputPin ──tiene──▶ Net ──lista──▶ InputPin ──pertenece a──▶ Gate
  │                                                                            ▲
  └──tiene──▶ InputPin[] ─────────────────────────────────────────────────────┘
```

Un `Net` es el cable: un excitador, muchos destinos. El ciclo de ese diagrama es el punto — un
`InputPin` conoce la compuerta a la que alimenta, y así un cambio de valor se convierte en una
decisión de agendado en el momento exacto en que se sabe que es un cambio.

`Port` es el raro. Hereda de `InputPin` pero tiene un `OutputPin` propio y vuelve a excitar lo que
le entra, que es lo que le permite a un `Module` presentar un borde sin exponer las compuertas de
adentro. Un puerto de entrada y uno de salida son el mismo tipo; la diferencia es nada más a qué
lista del módulo se lo agregó.

## Los módulos no se simulan

`Module` no tiene comportamiento alguno — no tiene `Evaluate`, no entra en la cola. Es un
dispositivo de nombrado y agrupación: puertos afuera, un grafo de compuertas adentro, y un
`ModuleLayout` que mapea nombres de señal a índices de puerto. En tiempo de ejecución un
`RippleCarryAdder` es indistinguible de las compuertas sueltas que lo componen.

Por eso la librería no tiene un nivel de "bloque primitivo". `JKFlipFlop` son ocho NAND, un AND y
dos inversores, y se comporta como uno porque *es* uno.

## Lo que falta a propósito

- **El tiempo.** Sin retardos, sin calendario de eventos, sin modelo temporal. `Run` te da el
  resultado estabilizado, así que los estados transitorios por los que pasa un circuito real son
  invisibles. Los valores intermedios de un contador asincrónico son reales y este simulador nunca
  te los va a mostrar.
- **Múltiples excitadores.** Un excitador por entrada. Sin contención de bus, sin alta impedancia,
  sin función de resolución.
- **Fuerza.** Sin pull-ups, sin colector abierto.

Cada una de esas es una omisión deliberada y no una función a medio hacer: agregar cualquiera
cambia la regla de propagación de "estabilizar" a "agendar", que es otro simulador.
