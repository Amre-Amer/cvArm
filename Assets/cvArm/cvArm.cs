using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cvArm : MonoBehaviour {
    GameObject[] segsTry;
    GameObject[] segsBest;
    GameObject[] segsReal;
    GameObject target;
    GameObject home;
    Vector3[] rotsTry;
    Vector3[] rotsMax;
    Vector3[] rotsMin;
    Vector3[] rotsBest;
    Vector3[] rotsReal;
    int numSegs = 7;
    float xSize = 1;
    float ySize = 3;
    float zSize = 10;
    Vector3 rotCurrentTry;
    Vector3 rotCurrentBest;
    Vector3 rotCurrentReal;
    int frameCount;
    float distBest;
    float distTry;
    [Range(0, 90)]
    public float targetZ = 60;
    Vector3 posTargetLast;
    GameObject bestGo;
    GameObject realGo;
    float range = 30;
    GameObject parentTry;
    GameObject parentBest;
    GameObject parentBreadCrumbs;
    float speedTarget = 1f;
    public bool ynManual;
    public bool ynStep;
    float timeStep;
    GameObject tryGo;
    int bestCount;
    float restartRange = 10;
    GameObject bestDistGo;
    GameObject restartRangeGo;
    float smooth = .95f;

	void Start () {
        initTarget();
        initSegs();
	}

    void Update()
    {
        if (ynStep == true) {
            if (Time.realtimeSinceStartup - timeStep > 1) {
                timeStep = Time.realtimeSinceStartup;
                UpdateOne();
            }
        } else {
            UpdateOne();
        }
    }

	void UpdateOne () {
        updateTarget();
        updateBestDist();
        updateLearning();
        //updateSegsTry();
        frameCount++;
	}

    void updateTarget() {
        if (ynManual == true) return;
        float s = .5f;
        float x = s * targetZ * Mathf.Cos(speedTarget * frameCount * Mathf.Deg2Rad);
        float y = s * targetZ * Mathf.Sin(speedTarget * frameCount * Mathf.Deg2Rad);
        target.transform.position = new Vector3(x, y, targetZ);
    }

    bool didTargetMove() {
        bool yn = false;
        if (target.transform.position != posTargetLast) {
            posTargetLast = target.transform.position;
            yn = true;
        }
        return yn;
    }

    void updateBestDist() {
        float dist = getDistTargetBest();
        if (dist > restartRange) {
            bestCount = 0;
            distBest = restartRange;
            target.GetComponent<Renderer>().material.color = Color.red;
            restartRangeGo.GetComponent<Renderer>().material.color = new Color(1, 0, 0, .25f);
        } else {
            target.GetComponent<Renderer>().material.color = Color.green;
            restartRangeGo.GetComponent<Renderer>().material.color = new Color(0, 1, 0, .25f);
        }
        bestDistGo.transform.position = target.transform.position;
        bestDistGo.transform.localScale = new Vector3(distBest * 2, distBest * 2, distBest * 2);
        bestDistGo.GetComponent<Renderer>().material.color = new Color(0, 0, 1, .25f);
        //
        restartRangeGo.transform.position = target.transform.position;
        restartRangeGo.transform.localScale = new Vector3(restartRange * 2, restartRange * 2, restartRange * 2);
    }

    void restartX() {
        bestCount = 0;
        distBest = 100 * Vector3.Distance(home.transform.position, target.transform.position);
    }

    void updateLearning() {
        updateRotTry();
        updateSegsTry();
        if (isRotTryBest() == true) {
            bestCount++;
            copyTryToBest(); 
            updateSegsBest();
        }
        updateSegsReal();
//        addBreadCrumb();
    }

    bool isRotTryBest()
    {
        bool yn = false;
        distTry = getDistTargetTry();
        if (distTry < distBest)
        {
            distBest = distTry;
            yn = true;
        }
        return yn;
    }

    void addBreadCrumb() {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.parent = parentBreadCrumbs.transform;
        go.transform.localScale = new Vector3(1, 1, 1);
        go.transform.position = getFront(getLastSeg(segsTry));
        Destroy(go, 20);
    }

    void copyTryToBest() {
        for (int s = 0; s < numSegs; s++)
        {
            rotsBest[s] = rotsTry[s];
        }
    }

    void updateRotTry() {
        for (int s = bestCount; s < numSegs; s++)
        {
            rotsTry[s].x = Random.Range(-range, range);
            rotsTry[s].y = Random.Range(-range, range);
            rotsTry[s].z = Random.Range(-range, range);
        }
    }

    GameObject getLastSeg(GameObject[] segs) {
        return segs[segs.Length - 1];
    }

    float getDistTargetTry() {
        return Vector3.Distance(target.transform.position, tryGo.transform.position);
    }

    float getDistTargetBest()
    {
        return Vector3.Distance(target.transform.position, bestGo.transform.position);
    }

    void updateSegsTry() {
        rotCurrentTry = Vector3.zero;
        for (int s = 0; s < numSegs; s++) {
            rotCurrentTry += rotsTry[s];
            updateSeg(segsTry, s, rotCurrentTry);
        }
        tryGo.transform.position = getFront(getLastSeg(segsTry));
   }

    void updateSegsReal()
    {        
        rotCurrentReal = Vector3.zero;
        for (int s = 0; s < numSegs; s++)
        {
            rotsReal[s] = smooth * rotsReal[s] + (1 - smooth) * rotsBest[s];
            rotCurrentReal += rotsReal[s];
            updateSeg(segsReal, s, rotCurrentReal);
        }
        realGo.transform.position = getFront(getLastSeg(segsReal));
    }

    void updateSegsBest()
    {
        rotCurrentBest = Vector3.zero;
        for (int s = 0; s < numSegs; s++)
        {
            rotCurrentBest += rotsBest[s];
            updateSeg(segsBest, s, rotCurrentBest);
        }
        bestGo.transform.position = getFront(getLastSeg(segsBest));
    }

    void updateSeg(GameObject[] segs, int s, Vector3 rot)
    {
        if (s == 0)
        {
            positionRotateFromRear(segs[s], getFront(home), rot);
        } else {
            positionRotateFromRear(segs[s], getFront(segs[s - 1]), rot);
        }
    }

    void positionRotateFromRear(GameObject go, Vector3 posRear, Vector3 quat)
    {
        go.transform.position = posRear;
        go.transform.eulerAngles = quat;
        go.transform.position += go.transform.forward * go.transform.localScale.z / 2;
    }

    void positionRotateTargetFromRear(GameObject go, Vector3 posRear, Vector3 posTarget)
    {
        go.transform.position = posRear;
        go.transform.LookAt(posTarget);
        go.transform.position += go.transform.forward * go.transform.localScale.z / 2;
    }

    Vector3 getFront(GameObject go)
    {
        return go.transform.position + go.transform.forward * go.transform.localScale.z / 2;
    }

    void initSegs(){
        rotsTry = new Vector3[numSegs];
        rotsMin = new Vector3[numSegs];
        rotsMax = new Vector3[numSegs];
        rotsBest = new Vector3[numSegs];
        rotsReal = new Vector3[numSegs];
        segsTry = new GameObject[numSegs];
        segsBest = new GameObject[numSegs];
        segsReal = new GameObject[numSegs];
        parentTry = new GameObject("parentTry");
        parentBest = new GameObject("parentBest");
        parentBreadCrumbs = new GameObject("parentBreadCrumb");
        for (int s = 0; s < numSegs; s++)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "segTry " + s;
            seg.transform.position = new Vector3(0, 0, 0);
            seg.transform.localScale = new Vector3(xSize, ySize, zSize);
            float c = (float)s / numSegs;
            seg.GetComponent<Renderer>().material.color = new Color(c, c, c);
            seg.transform.parent = parentTry.transform;
            rotsTry[s] = Vector3.zero;
            segsTry[s] = seg;
            segsTry[s].transform.eulerAngles = rotsTry[s];
            //
            GameObject segBest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segBest.name = "segBest " + s;
            segBest.transform.position = new Vector3(0, 0, 0);
            segBest.transform.localScale = new Vector3(xSize, ySize, zSize);
            float cBest = (float)s / numSegs;
            segBest.GetComponent<Renderer>().material.color = new Color(cBest, 1, cBest);
            segBest.transform.parent = parentBest.transform;
            rotsBest[s] = Vector3.zero;
            segsBest[s] = segBest;
            segsBest[s].transform.eulerAngles = rotsBest[s];
            //
            GameObject segReal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segReal.name = "segReal " + s;
            segReal.transform.position = new Vector3(0, 0, 0);
            segReal.transform.localScale = new Vector3(xSize, ySize, zSize);
            float cReal = (float)s / numSegs;
            segReal.GetComponent<Renderer>().material.color = new Color(1, cReal, cReal);
            segReal.transform.parent = parentBest.transform;
            rotsReal[s] = Vector3.zero;
            segsReal[s] = segBest;
            segsReal[s].transform.eulerAngles = rotsReal[s];
        }
        initHome();
    }

    void initHome() {
        home = GameObject.CreatePrimitive(PrimitiveType.Cube);
        home.name = "home";
        home.transform.localScale = new Vector3(1, 1, 1);
        home.GetComponent<Renderer>().material.color = Color.green;
        home.transform.position = new Vector3(0, 0, 0);
    }

    Vector3 getRear(GameObject go) {
        return go.transform.position = go.transform.forward * -1 * go.transform.localScale.z / 2;
    }

    void positionFrontX(GameObject go, Vector3 pos) {
        go.transform.position = pos;
        go.transform.position += go.transform.forward * -1 * go.transform.localScale.z / 2;
    }

    void positionRearX(GameObject go, Vector3 pos)
    {
        go.transform.position = pos;
        go.transform.position += go.transform.forward * go.transform.localScale.z / 2;
    }

    void initTarget() {
        target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = "target";
        target.transform.localScale = new Vector3(3, 3, 3);
        target.GetComponent<Renderer>().material.color = Color.green;
        target.transform.position = new Vector3(0, 0, targetZ);
        //
        bestGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bestGo.name = "bestGo";
        bestGo.transform.localScale = new Vector3(3, 3, 3);
        bestGo.GetComponent<Renderer>().material.color = Color.red;
        bestGo.transform.position = new Vector3(0, 0, 0);
        //
        tryGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tryGo.name = "tryGo";
        tryGo.transform.localScale = new Vector3(3, 3, 3);
        tryGo.GetComponent<Renderer>().material.color = Color.yellow;
        tryGo.transform.position = new Vector3(0, 0, 0);
        //
        realGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        realGo.name = "realGo";
        realGo.transform.localScale = new Vector3(3, 3, 3);
        realGo.GetComponent<Renderer>().material.color = Color.cyan;
        realGo.transform.position = new Vector3(0, 0, 0);
        //
        bestDistGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bestDistGo.name = "bestDistGo";
//        bestDistGo.transform.localScale = new Vector3(distBest * 2, distBest * 2, distBest * 2);
        makeMaterialTransparent(bestDistGo.GetComponent<Renderer>().material);
//        bestDistGo.GetComponent<Renderer>().material.color = new Color(0, 0, 1, .25f);
//        bestDistGo.transform.position = new Vector3(0, 0, 0);
        //
        restartRangeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        restartRangeGo.name = "restartRangeGo";
//        restartRangeGo.transform.localScale = new Vector3(restartRange * 2, restartRange * 2, restartRange * 2);
        makeMaterialTransparent(restartRangeGo.GetComponent<Renderer>().material);
        //restartRangeGo.GetComponent<Renderer>().material.color = Color.gray;
//        restartRangeGo.transform.position = new Vector3(0, 0, 0);
    }

    void makeMaterialTransparent(Material material) {
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}
