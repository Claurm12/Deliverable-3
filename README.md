# ***Deliverable-3***
Documentación de la primera interacción del proyecto
# ***Documentación***

## **Cómo ejecutar el proyecto**
Tanto el servidor como el cliente forman parte del mismo .exe. Al ejecutar el .exe aparecerá el menú principal, donde además de cambiar el nombre podremos crear partida convirtiéndonos en el host (parte de servidor) o unirnos a una partida que otra persona haya creado (parte del servidor)

### Versión de Unity
6000.3.16f1

## **Escena principal**
La primera escena que aparece al ejecutar el programa es la L3_Menu, de ahi va pasando por diferentes pantallas siguiendo este esquema:
   <p align="center">  L3_Menu </p>
   <p align="center">   ⬇️ </p>
     <p align="center"> L3_CreateGame / L3_JoinGame  </p>
   <p align="center">  ⬇️  </p>
  <p align="center"> L3_WaitingRoom  </p>
    <p align="center">  ⬇️  </p>
    <p align="center"> L3_Game  </p>

## **Input**
Se utilizan estos controles porque son los controles de movimiento típicos para juegos de PC.
### Movimiento
W y UpArrow -> Mover hacia arriba <br>
S y DownArrow -> Mover hacia abajo <br>
A Y LeftArrow -> Mover hacia la izquierda <br>
D y RightArrow -> Mover hacia la derecha <br>
Barra espaciadora -> Saltar <br>

### Atacar
F -> Botón para disparar
## **Extras**
* El jugador puede saltar
* Al iniciar la partida se asignan los roles de equipo de manera aleatoria.
* El jugador puede disparar
* Más de 2 jugadores

## **Cambios de la anterior entrega**

* Los datos del juego van por UDP.
* Pasamos de tener todo en una escena a tener una escena para el menú principal, otra para crear sala, otra para unirse a sala, una para la waiting room y otra para el juego.
* Cliente y servidor están en el mismo ejecutable en vez de 2 diferentes.
* Al unirse al juego, se asignan de manera aleatoria los roles de Cazador y Cazado, y se asigna su color a cada jugador.
* Escena de juego añadida, con pj para cada jugador en el servidor. Y una leyenda con los controles en pantalla.
* La serializacion esta implementada en su propio script.
* La UI de cada escena tiene su propio codigo.

## **Explicación latencia y mensajes**
Se mantiene el lag bajo pasando todas las acciones del juego por udp, mientras que los chats pasaran por tcp. De esa manera se mantiene bajo y podemos asegurarnos de que los mensajes siempre lleguen a todos los jugadores.

## **Contribución de cada miembro del equipo**
| **Miembro** | **Tareas realizadas** | 
| ------------- | ------------- |
| *Claudia Ruiz* | - Creación Proyecto Unity base <br> - Aparecen todos los jugadores <br> - Borrar jugador <br> - Movimiento jugador <br> - Asignación de roles <br> - Asignación colores por rol a los jugadores|
| *Víctor González* |- Creación Proyecto Unity base <br>- Readme <br> - Jugador no se mueve mientras se usa el chat <br> - El jugador salta <br> - El jugador dispara <br> - Limpieza de código <br> - Limpieza Ui <br> - Arreglo bug textura colores jugadores|

## **Authors**

- ***Cluadia Ruiz*** [@Claurm12](https://www.github.com/Claurm12)
- ***Víctor González*** [@TheWolfG145](https://www.github.com/TheWolfG145)
