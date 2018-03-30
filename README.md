# F.S.V.E (Fluid Simulated Volumetric Effects)

(Formally Fluid Simulated Volumetric Explosions)
A Unity integrated system capable of producing real-time ~~explosions~~ volumetric effects using fluid dynamic simulation principles and particle suspension, all implemented on the GPU using compute shaders. The system provides a number of fluid interactables such as emitters, containers and colliders, that allow the user to produce a variety of effects. The library breaks down the fluid simulation stages into modules, allowing the system to be easily extended to produce new simulations.

![alt text](/url/to/https://drive.google.com/file/d/1W1oo6GRBSamGZ8XAoJSbl5GPimxk8pt_/view?usp=sharing"Alchemy Bottle")

##Features
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
- Volume conversion (particles to 3D texture)
- 2D texture based fluid simulation (legacy no longer developed)

**Unity version:** 5.6.3f

**Date Created:** 23/10/2017