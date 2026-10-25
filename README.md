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

Els extres que heu fet

## **Bugs Conocidos**

Els bugs coneguts. Un bug que ja apunteu resta menys que un que trobem nosaltres


## **Cambios de la anterior entrega**

* Cliente y servidor solo de UDP, ya no hay TCP.
* Pasamos de tener todo en una escena a tener una escena para el menu principal, otra para crear sala, otra para unirse a sala, una para la witing room y otra para el juego.
* Cliente y servidor estan en el mismo ejecutable en vez de 2 diferentes.
* Al unirse al juego se da de manera aleatoria los roles de Cazador y Cazado, y se asigna su color a cada jugador.
* Pantalla de juego añadida, con pj para cada jugador en el servidor. y una leyenda con los controles en pantalla.

## **Contribución de cada miembro del equipo**
| **Miembro** | **Tareas realizadas** | 
| ------------- | ------------- |
| *Claudia Ruiz*  | - Creacion Proyecto unity base <br> -   |
| *Víctor González*  |- Base Readme  <br> -  |

## **Authors**

- ***Cluadia Ruiz*** [@Claurm12](https://www.github.com/Claurm12)
- ***Víctor González*** [@TheWolfG145](https://www.github.com/TheWolfG145)
