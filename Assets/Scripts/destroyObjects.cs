using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class destroyObjects : MonoBehaviour
{
    [SerializeField]
    private Material burn_mat;

    Renderer renderer;
    float progress = 0;
    private bool isColliding;

    private void OnTriggerEnter(Collider other)
    {
        renderer = other.gameObject.transform.parent.gameObject.GetComponent<Renderer>();
        //    //if (!renderer.material.HasProperty())
        //    //{
        //    //    gameObject.SetActive(false);
        //    //}
        if (renderer.material.HasProperty("_Color"))
        {
            Color c = renderer.material.color;
            renderer.material.SetColor(renderer.material.shader.GetPropertyNameId(1), c);
        }
            
        renderer.material = burn_mat;

        //other.gameObject.transform.parent.gameObject.GetComponent<Mesh>().RecalculateUVDistributionMetrics();
        //if (progress == 0)
        //{
        //    DestroyWithFlare(other.gameObject);
        //}


        //Destroy(other.transform.parent.parent.gameObject) ;
        // maybe first a warning
    }

    private void OnTriggerStay(Collider other)
    {
            // doesn't work properly if UVs are wrong or missing
           
       
        

        if (!isColliding)
        {
            isColliding = true;

            if (progress < 1)
            {
                print(progress);
                progress += Time.deltaTime * 4;
                renderer.material.SetFloat(renderer.material.shader.GetPropertyNameId(0), progress);

                // yield return new WaitForSeconds(0.05f);
            }
            else
            {
                progress = 0;
                Destroy(other.gameObject.transform.parent.parent.gameObject); ;
            }
        }
    }

    private void Update()
    {
        // reset every update/frame
        isColliding = false; 
    }

    //private void OnTriggerExit(Collider other)
    //{
    //    progress = 0;
    //    Destroy(other.gameObject.transform.parent.parent.gameObject); ;
    //}

    // Update is called once per frame
    //void DestroyWithFlare(GameObject obj)
    //{

    //    renderer = obj.GetComponent<Renderer>();
    //    //if (!renderer.material.HasProperty())
    //    //{
    //    //    gameObject.SetActive(false);
    //    //}
    //    Color c = renderer.material.color;
    //    renderer.material = burn_mat;
    //    //renderer.material.SetColor(renderer.material.shader.GetPropertyNameId(1), c);

    //    StartCoroutine(fire(obj));


    //}



    //IEnumerator fire(GameObject obj)
    //{

    //    while (progress < 1)
    //    {
    //        print(progress);
    //        progress += Time.deltaTime * 4;
    //        renderer.material.SetFloat(renderer.material.shader.GetPropertyNameId(0), progress);

    //        yield return new WaitForSeconds(0.05f);
    //    }
    //    progress = 0;
    //    Destroy(obj);
    //    yield return true;
    //}



}
