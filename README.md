# F.S.V.E (Fluid Simulated Volumetric Effects)

(Formally Fluid Simulated Volumetric Explosions)
A Unity integrated system capable of producing real-time ~~explosions~~ volumetric effects using fluid dynamic simulation principles and particle suspension, all implemented on the GPU using compute shaders. The system provides a number of fluid interactables such as emitters, containers and colliders, that allow the user to produce a variety of effects. The library breaks down the fluid simulation stages into modules, allowing the system to be easily extended to produce new simulations.

**Unity version:** 5.6.3f

**Date Created:** 23/10/2017

![alchemy_bottle](https://user-images.githubusercontent.com/18384589/38148255-937eb0fe-344d-11e8-961b-f6ec4fb3f6a0.png)

## Features
- Expandable base fluid simulation framework
- Custom simulation inspector (with framework for creating new simulation inspectors)
- Fluid simulated smoke effect
- Fluid manipulated particle simulation (Experimental)
- Fluid wrapping or restrict to bounds
- Dynamic interactor system (simulation auto manages interactables within simulation bounds)
- Fluid collisions (currently limited to sphere colliders)
- Fluid containers to restrict fluid to areas
- Smoke emitters for adding density to smoke simulation
- Generic volume ray-marcher for rendering any 3D texture
- Fluid volume instancing
- Volume colour randomisation
- Volume colour gradients, controlled by either height or density
- Advanced render setting in Volume renderer inspector, such as blend mode and Z testing
- Volume conversion (particles to 3D texture)
- 2D texture based fluid simulation (legacy no longer developed)

![instancing](https://user-images.githubusercontent.com/18384589/38148282-b2f765de-344d-11e8-8a66-0a49dc37717b.png)
![smoke collision](https://user-images.githubusercontent.com/18384589/38148295-bf9f4bb2-344d-11e8-9e27-500d27bf76b2.png)
![density gradient](https://user-images.githubusercontent.com/18384589/38209212-803e6f4c-36ab-11e8-9ff0-86734cbf0ac6.png)
![gradient height](https://user-images.githubusercontent.com/18384589/38209218-90966a3e-36ab-11e8-8cdd-d08c9088f302.png)



