//Written by Vili Volcini
/*
 * MIT License
 * 
 * 
 * Copyright(c) [year]
 * [fullname]
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sensor : MonoBehaviour {

    public LayerMask hitMask;

    public enum Type {

        LineCast,
        BoxCast,
        SphereCast,
        CheckBox, // doesn't give you normals (info about collision), just boolean (nothing hit / something hit)
        CurvedCast,
        FullBoxCast,
    }

    public Type raycastType = Type.LineCast;
    public float raycastLength=1f;
    public float raycastThin=1f;
    
    Transform cachedTransform;
     
    [Space]
    [Header("CurvedCast settings")]
    [Range(0, 10)]
    public int resolution = 5;

    [Range(0, Mathf.PI * 2f)]
    public float arcAngle = Mathf.PI*(3f/2f);

    [Range(0, 2f)]
    public float radius = 0.5f;

    [Space]
    [Header("BoxCast and CheckBox settings")]

    public bool customThinDimension;
    public Vector2 thinDimension;

	void Awake () {
        cachedTransform = GetComponent<Transform>();
	}

    // was anything hit on Scan()?
    public bool Hit {
        get;
        private set;
    }
    
    // made private, was public
    // more boxSlices = more performance intensive but more precision
    // less boxSlices = less performance intensive but less precision
    int boxSlices = 12;

    [System.NonSerialized]
    // returns info of closest collider it was hit
    public RaycastHit info = new RaycastHit();
    
    // returns [0..1]
    // 0 means nothing was hit (farest away from source)
    // 1 means closest to source of sensor
    // can be used to calculate how close sensor is to collider it has hit
    public float Percent() {
        
        if (Hit) {
            return ( raycastLength- (info.point - this.cachedTransform.position).magnitude)
                    / raycastLength;
        }
        else
            return 0f;
    }

    public float DistanceFromStart() {

        if (Hit) {
            return (info.point - this.cachedTransform.position).magnitude;
        }
        else
            throw new UnityException("there was nothing Hit, cannot measure distance. Please check first, if sensor was hit!");
    }

    public RaycastHit[] hits;
    public bool Scan() {

        //reset Hit
        Hit = false;

        //direction of cast depends on transform
        Vector3 dir = cachedTransform.forward;
        
        switch (raycastType) {

            case Type.LineCast:
                if (Physics.Linecast(cachedTransform.position, cachedTransform.position + dir * raycastLength, out info, hitMask, QueryTriggerInteraction.Ignore)) {

                    Hit = true;
                    return true;
                }
            break;

            case Type.BoxCast:
                if (customThinDimension)
                    if (Physics.BoxCast(cachedTransform.position, new Vector3(thinDimension.x, thinDimension.y, raycastLength / boxSlices) / 2f, dir, out info, cachedTransform.rotation, raycastLength - (raycastLength / boxSlices) / 2f, hitMask, QueryTriggerInteraction.Ignore)) {

                        Hit = true;
                        return true;
                    }
                else
                    if (Physics.BoxCast(cachedTransform.position, new Vector3(raycastThin, raycastThin, raycastLength / boxSlices) / 2f, dir, out info, cachedTransform.rotation, raycastLength - (raycastLength / boxSlices) / 2f, hitMask, QueryTriggerInteraction.Ignore)) {

                        Hit = true;
                        return true;
                    }
            break;

            case Type.FullBoxCast:
                if (customThinDimension)
                    hits = Physics.BoxCastAll(cachedTransform.position, new Vector3(thinDimension.x, thinDimension.y, raycastLength / boxSlices) / 2f, dir, cachedTransform.rotation, raycastLength - (raycastLength / boxSlices) / 2f, hitMask, QueryTriggerInteraction.Ignore);
                else
                    hits = Physics.BoxCastAll(cachedTransform.position, new Vector3(raycastThin, raycastThin, raycastLength / boxSlices) / 2f, dir, cachedTransform.rotation, raycastLength - (raycastLength / boxSlices) / 2f, hitMask, QueryTriggerInteraction.Ignore);

                // sort hits by distance
                if (hits.Length > 0) {
                    System.Array.Sort(hits, (s1, s2) => {

                        if (s1.distance > s2.distance)
                            return 1;

                        if (s2.distance > s1.distance)
                            return -1;

                        return 0;
                    });
                    
                    Hit = true;

                    //set closest collider here
                    info = hits[0];
                    return true;
                }
                
            break;

            case Type.SphereCast:

                if (Physics.SphereCast(new Ray(cachedTransform.position, dir), raycastThin, out info, raycastLength, hitMask,QueryTriggerInteraction.Ignore)) {

                    Hit = true;
                    return true;
                }
            break;

            case Type.CheckBox:
                if (customThinDimension) {
                    if (Physics.CheckBox(this.transform.position, new Vector3(thinDimension.x, thinDimension.y, raycastLength) / 2f, transform.rotation, hitMask, QueryTriggerInteraction.Ignore)) {

                        Hit = true;
                        return true;
                    };
                }
                else {
                    if (Physics.CheckBox(this.transform.position, new Vector3(raycastThin, raycastThin, raycastLength)/2f, transform.rotation, hitMask,QueryTriggerInteraction.Ignore)) {

                        Hit = true;
                        return true;
                    };
                }
            break;
           
            case Type.CurvedCast:          
                float step = arcAngle / resolution;

                Vector3 origin = this.transform.position - transform.forward * radius;
                Vector3 _x, _y;

                //calculate arc, cast around it
                for (int i = 0; i < resolution; i++) {

                    float prevAngle = step * (i);
                    float nextAngle = step * (i + 1);

                    _x = transform.forward;
                    _y = transform.up;

                    Vector3 prevDir = Mathf.Cos(prevAngle) * _x + Mathf.Sin(prevAngle) * _y;
                    Vector3 nextDir = Mathf.Cos(nextAngle) * _x + Mathf.Sin(nextAngle) * _y;

                    prevDir *= radius;
                    nextDir *= radius;

                    prevDir += origin;
                    nextDir += origin;

                    // hit something, stop!
                    if (Physics.Linecast(prevDir, nextDir, out info, hitMask, QueryTriggerInteraction.Ignore)) {
                        Hit = true;
                        return true;
                    }
                }
            break;
        }

        return false;
    }

    // Rename to "OnDrawGizmosSelected" if you wish that Sensors are drawn only when you select Prefab. Less noise that way and with a lot of Sensors might cause performance issues with Scene view inside Editor
    void OnDrawGizmos() {

        Gizmos.color = Color.white;

        if (cachedTransform == null)
            cachedTransform = GetComponent<Transform>();

        // scan the world
        Scan();

        if (Hit)
            Gizmos.color = Color.red;
        
        // transform the gizmo
        Gizmos.matrix *= Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        var length = raycastLength;

        switch (raycastType) {

            case Type.LineCast:

                if (Hit)
                    length = Vector3.Distance(cachedTransform.position, info.point);
                
                var p = cachedTransform.position;

                Gizmos.DrawLine(Vector3.zero, Vector3.forward * length);
                
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.02f, 0.02f, 0.02f));
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(Vector3.forward * length, new Vector3(0.02f, 0.02f, 0.02f));

            break;

            case Type.BoxCast:
            case Type.FullBoxCast:

                if (Hit)
                    length = info.distance;
                
                if (customThinDimension) {
                  
                    // Gizmos.DrawWireCube(Vector3.zero, new Vector3(thinDimension.x, thinDimension.y, length));
                    var scaledVec = new Vector3(1f, 1f, 0.1f);
                        scaledVec.Scale(new Vector3(thinDimension.x, thinDimension.y, 1));

                    Gizmos.DrawWireCube(new Vector3(0, 0, 1) * length / 2, new Vector3(thinDimension.x, thinDimension.y, length));
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(Vector3.zero, scaledVec);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(new Vector3(0, 0, 1) * length, scaledVec);                
                }
                else {

                    Gizmos.DrawWireCube(new Vector3(0, 0, 1) * length / 2, new Vector3(raycastThin, raycastThin, length));
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 1f, 0.1f) * raycastThin);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(new Vector3(0, 0, 1) * length, new Vector3(1f, 1f, 0.1f) * raycastThin);
                }
                
            break;

            case Type.CheckBox:

                if (customThinDimension) {

                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(thinDimension.x, thinDimension.y, length));
                }
                else {

                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(raycastThin, raycastThin, length));
                }
            break;

            case Type.SphereCast:

                Gizmos.DrawWireSphere(Vector3.zero, raycastThin);

                var halfThin = raycastThin;

                if (Hit) {

                    var ballCenter = info.point + info.normal * raycastThin;
                    length = Vector3.Distance(cachedTransform.position, ballCenter);
                }

                Gizmos.DrawLine(Vector3.up     * halfThin,  Vector3.up    * halfThin + Vector3.forward * length);
                Gizmos.DrawLine(-Vector3.up    * halfThin, -Vector3.up    * halfThin + Vector3.forward * length);
                Gizmos.DrawLine(Vector3.right  * halfThin,  Vector3.right * halfThin + Vector3.forward * length);
                Gizmos.DrawLine(-Vector3.right * halfThin, -Vector3.right * halfThin + Vector3.forward * length);

                Gizmos.DrawWireSphere(Vector3.zero + Vector3.forward * length, raycastThin);

                if (Hit) {

                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.DrawLine(info.point, info.point + info.normal * (raycastThin / 2f));
                }

            break;

            case Type.CurvedCast:
                
                Gizmos.matrix = Matrix4x4.identity;
                
                float step = arcAngle / resolution;

                Vector3 origin = this.transform.position - transform.forward * radius;
                Vector3 _x, _y;
                
                // draw an arc
                for (int i = 0; i < resolution; i++) {

                    float prevAngle = step * (i    );
                    float nextAngle = step * (i + 1);

                    _x = transform.forward;
                    _y = transform.up;
                    
                    Vector3 prevDir = Mathf.Cos(prevAngle) * _x  +  Mathf.Sin(prevAngle) * _y;
                    Vector3 nextDir = Mathf.Cos(nextAngle) * _x  +  Mathf.Sin(nextAngle) * _y;

                    prevDir *= radius;
                    nextDir *= radius;

                    prevDir += origin;
                    nextDir += origin;

                    // if something was hit something, stop!
                    if (Physics.Linecast(prevDir, nextDir, out info, hitMask)) {

                        Gizmos.DrawLine(prevDir   , info.point);
                        
                        //green box
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(info.point, info.point + info.normal * 0.1f);

                        Gizmos.DrawWireCube(Vector3.forward * length, new Vector3(0.02f, 0.02f, 0.02f));
                        
                        break;
                    }
                    else {

                        Gizmos.DrawLine(prevDir, nextDir);

                        if (i == resolution-1) {
                            //green box
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(Vector3.forward * length, new Vector3(0.02f, 0.02f, 0.02f));
                        }
                    }
                }

            break;
        }
    }
}
