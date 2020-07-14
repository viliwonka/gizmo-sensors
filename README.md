# Gizmo Sensors #

This code is for Unity engine.

Those sensors use Unity Physics API and they provide Gizmos which work inside Editor - so you can preview what sensors are doing.

Useful for anyone to see in 'real-time' how Raycasting works in Unity.

![alt-text](https://github.com/viliwonka/gizmo-sensors/blob/master/AllTypesOrto.PNG "Types from left to right")

### Types of sensors ###

* LineCast    (Physics.Linecast)
* BoxCast     (Physics.BoxCast)
* SphereCast  (Physics.SphereCast)
* CheckBox    (Physics.CheckBox)
* CurvedCast  (Physics.Linecast - multiple calls)
* FullBoxCast (Physics.BoxCastAll)

### How do I use this? ###

* Single script (Sensor.cs),

In inspector
> * Add Sensor.cs to GameObject,
> * Set "Hit Mask" (LayerMask),
> * Select sensor type in "Raycast Type",
> * Change other settings to change shape of your Sensor,

In code
> * Get reference to Sensor by calling "var s = GetComponent<Sensor>()" or making Sensor s public variable and setting reference to it
> * Call "s.Scan()" from code whenever you wish to scan,
> * use "s.Hit" boolean to check if Sensor has hit anything,
> * FullBoxCast specific - "s.hits" hold all RaycastHits after you call "s.Scan()"

### Why is it useful for? ###

* We use LineCast on NPCs for collision avoidance,
* FullBoxCast is used as a damage area where NPCs can give damage,
* LineCast is used for Foundations (Building) to scan for ground,
* LineCast is used for Props placement to scan the surface,
* CheckBox is used for all buildable things - to check if there is enough space / volume,
* CurvedCast was used for scanning ground by wall-walking NPCs. They were removed out of game now.

### How does it work? ###

* OnDrawGizmos() calls Scan() every Editor frame,
* some transformation calculations are done,
* Gizmos API is used to draw Gizmos,
* OnDrawGizmos() is not called when you build your game.
* Otherwise, inside game there are never calls to Scan() function. Sensor is static - has no Update() function. 

### Possible drawbacks ###
* Sensors are not sensitive to scale of Transform. So if you plan to scale your prefabs, sensors will not,
* Sensors ignore colliders marked as "Triggers",
* Many Sensors will make your Scene view (inside Editor) lag. Rename "OnDrawGizmos()" to "OnDrawGizmosSelected()" - it will draw gizmos only on currently selected prefab.

### In action ###
![alt-text](https://github.com/viliwonka/gizmo-sensors/blob/master/inAction.gif "Types from left to right")

### Licence ###
MIT licence. Full details in licence.txt file.
