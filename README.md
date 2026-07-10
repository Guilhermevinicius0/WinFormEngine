
#  Game Engine 2d | Windows Forms

[En]

This project aims to create a non-commercial open-source engine that enables the creation of games and physical simulations in a 2D environment in a simple way, seeking the feeling of creating scripts in established commercial engines such as Unity, Godot and GameMaker.

This is a study project done purely as a hobby to test my knowledge in C# and my ability to create abstractions and represent concepts.
## Ready Features
- AABB Collision.
- Programmable Camera.
- Editable Map.
- Keyboard and mouse input support.
- Rendering and physics optimization through spatial grid.
- Optimized rendering with bitmap and culling of objects outside the camera.
- Optimized loop based on idle time of the Windows message queue.
- Debug menu for real-time metrics
- Base objects for creating complex objects (such as player, enemy, etc..) with support for scripts that run on object creation (OnCreate()), every frame if the object is defined as non-static (OnStep(delta)), and when the object is destroyed (OnDestroy()).
- Z-index support for drawing different objects in order.
- Support for methods that approximate scripting to that of major engines such as: IsOnFloor(), MoveAndCollide(), GetCenteredPosition(), etc..
- Raycast support with list parameters to not detect, list to filter, and collide with the emitter, returns a raycastResult.
- Support for dynamic tags for entities and objects with O(n) search to filter types of objects such as coins, enemies or traps for example easily.
- Per-axis collision resolution with overlap calculation.

## Planned Features
- Migrate the rendering system to gpu.
- Add visual editors for tileMap, entity creation, and script window.
- Add more api methods that facilitate script development such as: MoveAndSlide(), IsOnCeiling(), IsOnWall(), GetLastColliderNormal(), etc..
- Scene manager.
- Support for scene serialization and deserialization.
- Particle system.
- Support for sprites and animations.
- Sound system.


## Appendix




## Authors

- [@GuilhermeVinicius](https://github.com/Guilhermevinicius0)


## Demo





[BR]

Este projeto tem como objetivo a criação de uma engine não comercial de codigo aberto que possibilite a criação de jogos e simulações físicas em um abiente 2d de forma simples, buscando a sensação de criar scripts em engines comerciais já estabelecidas como Unity, godot e gamemaker

Este é um projeto de estudos feito por puro hobby para testar os meus conhecimentos em c# e minha capacidade de criar abstrações e representar conceitos.

## Features prontas

- Colisão AABB.
- Camera proramavel.
- Mapa editaval.
- Suporte a inputs de teclado e mouse.
- otimização de Renderização e fisica através de spatial grid.
- Renderização otimizada com bitmap e culling de objetos fora da camera.
- Loop otimizado baseado no tempo idle da fila de mensagem do windows.
- Menu de debug para métricas em tempo real
- objetos base para a criação de objetos complexo (como player, enemy, etc..) com suporte a scripts que executam na criação do objeto (OnCreate()), a cada frame caso o objeto se defina como não estático (OnStep(delta)), e quando o objeto é destruido (OnDestroy()).
- Suporte a indexaxão Z para desenhar em orderns objetos diferentes.
- Suporte a metodos que aproximam o script ao script das grandes engines como: IsOnFloor(), MoveAndCollide(), GetCenteredPosition(), etc..
- Suporte a raycast com parametros de lista para não detectar, lista para filtrar, e colidir no emissor, retorna um raycastResult.
- Suporte a tags dinamicas para entidades e objetos com busca o(n) para filtrar tipos de objetos como moedas, inimigos ou armaldilhas por exemplo de forma fácil.

- Resolução de colisão por eixo com cálculo de overlap.

## Features Planejadas

- Migrar o sistema de renderização para gpu.
- Adicionar editoeres visuais para o tileMap, criação de entidades, e janela de script.
- Adicionar mais metodos de api que facilitam o desenvolvemnto dos scripts como os metodos: MoveAndSlide(), IsOnCeiling(), IsOnWall(), GetLastColliderNormal(), etc..
- Gerenciador de cenas.
- Suporte a serialização e deserialização de cenas.
- Sistema de particulas.
- Suporte a sprites e animações.
- Sistema de som.


## Appendix




## Authors

- [@GuilhermeVinicius](https://github.com/Guilhermevinicius0)


## Demo



