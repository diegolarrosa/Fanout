# Primeros pasos

En todo programa con Fanout pasan tres cosas: se arma un circuito, se excitan sus entradas y se le
pide que se estabilice. Esta página cubre las tres.

## Armar

Un `Circuit` es la raíz del árbol. Es dueño de la cola de trabajo, así que un puerto solo agenda
trabajo si el circuito lo conoce.

```csharp
using Fanout;
using Fanout.Gates;

var circuito = new Circuit();
var compuerta = new And();

Port a = circuito.AddInput();
Port b = circuito.AddInput();

a.ConnectTo(compuerta, 0);
b.ConnectTo(compuerta, 1);
```

`ConnectTo` siempre se lee del excitador hacia el destino: *esta salida va a esa entrada*. Un
excitador puede conectarse a todas las entradas que haga falta —eso es un `Net`—, pero una entrada
solo puede estar excitada por una cosa. Conectar dos excitadores a la misma entrada no es un error
ni un cortocircuito: el segundo simplemente pisa al primero al propagarse, que casi nunca es lo
que uno quiere.

## Excitar y estabilizar

```csharp
a.SetState(LogicState.One);
b.SetState(LogicState.One);

int rondas = circuito.Run();

Console.WriteLine(compuerta.Output.State);   // One
```

`SetState` sobre un puerto del nivel superior encola lo que ese puerto alcanza. `Run` recorre
después la cola hasta que no cambia nada y devuelve cuántas rondas llevó. Volver a llamar a `Run`
sin nada pendiente devuelve `0` y no hace trabajo, así que es barato llamarlo por las dudas.

Excitar un puerto con el valor que ya tiene no es un cambio y no agenda nada. Esta es la regla
sobre la que se apoya todo el motor, y conviene interiorizarla temprano: **el simulador responde a
diferencias, no a escrituras.**

## Módulos y nombres de señal

Cualquier cosa más grande que un par de compuertas debería ser un `Module`. Los módulos tienen
puertos con nombre, así que el circuito se lee como un esquemático y no como indexado de arreglos:

```csharp
using Fanout.Combinational;

var sumador = new RippleCarryAdder(width: 8);

Port acarreoEntrada = circuito.AddInput();
acarreoEntrada.ConnectTo(sumador, "CIN");

Port bit3 = circuito.AddInput();
bit3.ConnectTo(sumador, "A", 3);      // bit 3 del bus A
```

Leer funciona igual:

```csharp
LogicState alto = sumador.Output("SUM", 7).State;
LogicState acarreo = sumador.Output("COUT").State;
```

El bit 0 es siempre el menos significativo. Pedir un nombre que el módulo no tiene lanza una
`KeyNotFoundException` que lista los nombres que sí existen.

## Indeterminado es una respuesta de verdad

Un circuito recién armado está entero en `LogicState.Unknown`, y hay partes que pueden quedarse
así:

```csharp
var latch = new NorSRLatch();
// ... conectar R y S, poner las dos en bajo, correr ...

latch.Output("Q").State;   // Unknown — y está bien
```

Un latch de compuertas cruzadas con las dos entradas inactivas genuinamente no tiene estado
definido hasta que algo lo fuerza. Fanout lo dice, en vez de elegir un valor por vos. En la
práctica esto significa **pulsar la línea de borrado antes de relojear cualquier cosa secuencial**:

```csharp
borrado.SetState(LogicState.Zero);   // activo por bajo
circuito.Run();
borrado.SetState(LogicState.One);
circuito.Run();
```

Si un circuito secuencial lee `Unknown` donde esperabas un cero, lo primero que hay que revisar es
si falta ese pulso de borrado.

## Relojear

No hay generador de reloj, porque no hay tiempo. Un flanco es simplemente un cambio de valor
seguido de una estabilización:

```csharp
reloj.SetState(LogicState.Zero);
circuito.Run();
reloj.SetState(LogicState.One);    // el flanco de subida
circuito.Run();
```

Estabilizar después de cada transición. Bajar y subir sin un `Run` en el medio colapsa las dos en
un solo evento y el flanco nunca ocurre.

## Escribir una compuerta propia

Se hereda de `Gate` y se implementa `Evaluate`:

```csharp
public sealed class Mayoria : Gate
{
    public Mayoria() : base(3) { }

    protected override void Evaluate(GateQueue cola)
    {
        int altas = 0;

        for (int i = 0; i < InputCount; i++)
        {
            if (GetInputState(i) == LogicState.One)
            {
                altas++;
            }
        }

        Output.SetState(altas >= 2 ? LogicState.One : LogicState.Zero, cola);
    }
}
```

`Evaluate` se llama solo cuando la compuerta tiene información suficiente, así que nunca tiene que
manejar `Unknown`. Si tu compuerta tiene un valor controlante —una compuerta de mayoría lo tiene,
apenas dos entradas coinciden— conviene sobrescribir también `HasUnknownInputs` y se resolverá
antes. En [Cómo funciona](arquitectura.md) está lo que eso compra.

## Escribir un módulo propio

Se declara una disposición de puertos, se arma el interior en el constructor y se reenvían las dos
búsquedas por índice:

```csharp
public sealed class SemiSumador : Module
{
    public static readonly ModuleLayout Layout = new ModuleLayout()
        .AddInputGroup("A", 0)
        .AddInputGroup("B", 1)
        .AddOutputGroup("SUM", 0)
        .AddOutputGroup("COUT", 1);

    public SemiSumador()
    {
        Port a = new(), b = new(), suma = new(), acarreo = new();
        Xor xor = new();
        And and = new();

        AddInput(a);
        AddInput(b);
        AddOutput(suma);
        AddOutput(acarreo);

        a.ConnectTo(xor, 0);
        a.ConnectTo(and, 0);
        b.ConnectTo(xor, 1);
        b.ConnectTo(and, 1);
        xor.ConnectTo(suma);
        and.ConnectTo(acarreo);
    }

    public override int InputIndex(string name) => Layout.InputIndex(name);

    public override int OutputIndex(string name) => Layout.OutputIndex(name);
}
```

El orden en que se llama a `AddInput` y `AddOutput` **es** el orden de los puertos, y la
disposición tiene que coincidir con él. Ese es el único lugar donde un error queda mudo, así que
vale una prueba.

Para circuitos generados a partir de datos y no escritos a mano, está `CustomModule`, que recibe su
disposición como argumento del constructor en vez de necesitar un tipo.
