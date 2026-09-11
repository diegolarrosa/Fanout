# Escala y memoria

Esta librería guarda un circuito como un grafo de objetos .NET. Es la decisión correcta para los
circuitos a los que apunta, y es también lo que decide cuán grande puede llegar a ser un circuito.
Esta página saca la cuenta en vez de dejarla implícita.

## Cuánto cuesta una compuerta

Un `And` de dos entradas, en .NET de 64 bits, no es un objeto. Son nueve:

| Objeto | Bytes aproximados |
|---|---|
| El `And` en sí (cabecera, tres referencias, una bandera) | 48 |
| `List<InputPin>` y su arreglo de respaldo | 72 |
| `List<bool>` y su arreglo de respaldo | 56 |
| Dos instancias de `InputPin` | 64 |
| `OutputPin` | 32 |
| `Net` | 24 |
| `List<InputPin>` adentro del net, y su arreglo | 88 |
| **Total** | **≈ 380 bytes en 9–11 objetos** |

Son estimaciones sacadas de la disposición de campos, no mediciones — el redondeo del asignador y
el crecimiento de las listas los mueven. Lo que importa es el orden de magnitud: **unos cientos de
bytes y alrededor de diez objetos por compuerta.**

## Dónde queda el techo

| Compuertas | Memoria aproximada | Objetos vivos |
|---|---|---|
| 10 mil | 4 MB | 100 mil |
| 1 millón | 380 MB | 10 millones |
| 10 millones | 3,8 GB | 100 millones |

La memoria no es la restricción que manda: es la cantidad de objetos. Cien millones de objetos
vivos significan que cada recolección de generación 2 tiene que recorrer cien millones de
referencias, y el grafo está lleno de enlaces cruzados, así que nada de eso es barato de escanear.
Mucho antes de que se acaben los bytes, se acaba el recolector.

En la práctica este diseño es cómodo hasta **cientos de miles de compuertas** y penoso por encima
de eso. Todo lo que la librería efectivamente trae —un sumador, un banco de registros, un
contador, un camino de datos chico— vive muy por debajo de la línea.

## Qué compraría una representación empaquetada

La alternativa es dejar de usar objetos para los elementos del circuito y ponerlos en arreglos
planos, con handles enteros en lugar de referencias. A grandes rasgos:

| Elemento | Tamaño empaquetado |
|---|---|
| Una compuerta: tipo, cantidad de entradas, handle a la primera entrada, handle de salida, banderas | 16 bytes |
| Una entrada: estado, polaridad, handle de la compuerta dueña | 6 bytes |
| Una salida: handle al primer destino, cantidad | 8 bytes |
| Una entrada de fan-out | 5 bytes |

Una compuerta de dos entradas con un fan-out promedio de dos da **unos 50 bytes y cero objetos de
GC** — digamos entre seis y ocho veces menos memoria y, más importante, nada en absoluto para que
el recolector recorra. Diez millones de compuertas pasan a ser unos cientos de megabytes de
arreglos que el runtime nunca camina.

Esa técnica —una arena con handles tipados— es común en C++ y en Rust y rara en .NET, donde además
la biblioteca base está limitada a índices `int`. [Holdfast](https://www.nuget.org/packages/Holdfast) es una implementación de esto en .NET, del mismo autor
que esta librería.

## Por qué Fanout no lo hace

Dos razones, y la segunda es la de verdad.

**No haría más rápida la simulación.** La propagación es recorrido de punteros sobre un grafo:
cada paso lee un valor, sigue un enlace, lee el siguiente. Eso está limitado por la latencia de
memoria, que casi no se movió en quince años — del orden de 50 a 80 nanosegundos por fallo, sea
cual sea la representación. Empaquetar los datos hace que un circuito grande *entre*; no hace que
uno chico sea *rápido*.

**Le costaría a esta librería justamente lo que la hace legible.** El constructor de `FullAdder`
son doce líneas de `ConnectTo` que se leen igual que un esquemático. Bajo una arena eso pasa a ser
aritmética de handles, y la persona que vino a ver cómo se cablea un sumador completo se iría sin
haber aprendido nada. El público de un simulador legible a nivel de compuerta y el público de un
motor de diez millones de compuertas no son la misma gente, y tratar de servir a los dos con un
solo tipo no serviría a ninguno.

Entonces: si hacen falta circuitos de esa escala, la respuesta honesta es que esta no es la
librería, y que el motor empaquetado es otro proyecto que comparte la semántica de este —tres
estados, evaluación por valor controlante, estabilización en anchura— y no su API.

## Sacarle lo mejor a lo que hay

Algunas cosas sí ayudan dentro del diseño de grafo de objetos:

- **Reusar un circuito entre estímulos.** Armarlo es la parte cara; volver a excitarlo no. Todas
  las pruebas exhaustivas de este repositorio arman un circuito y hacen un lazo sobre las entradas.
- **Tomar `Circuit.LastRunRounds` solo como señal de costo.** Es barato de leer, pero no es
  profundidad lógica — ver [Cómo funciona](arquitectura.md#las-rondas-no-son-profundidad).
- **Darle a las compuertas anchas su ancho de entrada.** `new And(8)` asigna una vez; agregar ocho
  entradas de a una hace crecer una lista.
- **No crear un `Module` por bit** donde alcanza con un solo bloque ancho. El costo por módulo es
  un puñado de puertos, pero se acumula a lo largo de un camino de datos ancho.
