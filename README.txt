En aquest treball, hem après com fer un sistema multijugador en Unity 
utilitzant Unity transport seguint l'exemple de la documentació de SimpleClientServer. 

Seguint el codi proporcionat per les parts del client i servidor 
i adaptant-les a les configuracions de l'aula 
(com que l'aula no té ports oberts, hem utilitzat el localhost en la mateixa màquina 
i hem obert diferents projectes de Unity, simulant com si fossin dues màquines diferents).

Després, hem utilitzat les PIPELINES de Unity perquè els missatges es puguin trossejar 
(per si aquests poguessin ser molt llargs) i hem fet que un cop connectat el primer client amb el servidor, 
aquest últim enviés un missatge utilitzant BeginSend, EndSend 
i Read amb les dades de la connexió (nom del servidor, del client que li hagi posat aquest
i del temps que porta encès) 
i quan es conneta el segon client amb el servidor, aquest li enviï un missatge amb la mateixa estructura però també amb el nom del primer client connectat.


SEGONA PART DEL TREBALL 

En aquesta segona part s'ha eliminat tot el que formava part dels missatges de la primera part (pero el tema de les PIPELINES obviament s'ha mantingut)

S'ha fet tot el funcionament que es descriu en el document, on el servidor envia els personatges disponibles i el client els escolleix , els canvis d'escena pertinents 
I el bloqueig de seleccio de personatge si ja esta seleccionat.

En aquesta part ocorreix un problema 
A vegades un dels clients no pot rebre missatges del servidor (confirmacions de seleccio, personatges disponibles, etc) fins que un altre client que si pot n'envii algun cap al servidor
Per tant aquest client que no pot rebre no reacciona i executa les accions que s'han de fer quan rep missatges (canvi d'escena cap al personatge seleccionat principalment)

El servidor si rep tots els seus missatges, pero el client no rep les respostes
No hem aconseguit reproduir l'error consistenment pero creiem que es quan es fa la connexio en local, tal i com passava amb la primera part que s'enviaven multiples missatges al client sols quan era en local

TERCERA PART DEL TREBALL

L'error de lultima part ha estat solucionat, i s'ha seguit el que deia el document

NOTA IMPORTANT : No esta controlat que un client entri al joc abans que el segon es connecti per primera vegada
quan aixo passa hi han errors, ho arreglarem per al treball, per ara, assegurar-se que s'han connectat els dos clients abans de
seleccionar cap personatge.

Sobre els punts d'aquesta part 

1. Nivell creat, hi ha varies plataformes , es el mateix nivell al servidor i es representen les posicions de tots els clients
2, 3, Les posicions inicials es defineixen per l'array de transforms initialPositions al servidor, i quan es seleccionen personatges als clients 
es gestiona el Spawn dels players desde el servidor cap a cada client, tenint en conta que nomes s'han de spawnejar quan s'entra a la escena de joc
I que el player del client no es el mateix que els personatges instanciats desde el servidor 

4. Cal afegir el moviment del personatge
i. S’ha d’utilitzar RigidBody2D i les físiques del Unity3D : fet
ii. La posició real s’ha de calcular al servidor : fet
iii. La posició s’ha de simular al client i el servidor ha de corregirlo : fet, es te un maxDistanceThreshold i si la distancia entre la ultima posicio 
que s'ha enviat desde el client i la nova posicio es major que aquest threshold es capa la distancia moguda a aquest threshold en la direccio 
en la que anava el client

QUARTA PART DEL TREBALL

L'error de l'ultima part no ha estat solucionat encara 

Sobre els punts d'aquesta part 

1. Afegir enemic a l’escena
2. Afegir el comportament de l’enemic
3. Afegir la interacció entre l’enemic i els personatges

Els tres fets, el tercer solament es fan Debug.Logs quan ocorren colisions, al treball es fara la lógica de perdre vida 




