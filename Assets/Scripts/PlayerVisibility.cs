using System.Collections.Generic;
using UnityEngine;

public class PlayerVisibility : MonoBehaviour
{
    public Transform player;
    public Vector3 offest;
    [SerializeField] int layerNumber;
    [SerializeField]
    private List<Transform> ObjectToHide = new List<Transform>();
    private List<Transform> ObjectToShow = new List<Transform>();
    private Dictionary<Transform, Material> originalMaterials = new Dictionary<Transform, Material>();
    public Material transparentMaterial;
    [SerializeField] float obstructionFadingSpeed;

    void Start()
    {
    }

    private void LateUpdate()
    {
        ManageBlockingView();

        foreach (var obstruction in ObjectToHide)
        {
                HideObstruction(obstruction);
        }

        foreach (var obstruction in ObjectToShow)
        {
            ShowObstruction(obstruction);
        }
    }

    void Update()
    {
     
    }
   
    void ManageBlockingView()
    {
        Vector3 playerPosition = player.transform.position + offest;
        float characterDistance = Vector3.Distance(transform.position, playerPosition);
        int layerMask = 1 << layerNumber;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, playerPosition - transform.position, characterDistance, layerMask);
        if (hits.Length > 0)
        {
            // Repaint all the previous obstructions. Because some of the stuff might be not blocking anymore
            foreach (var obstruction in ObjectToHide)
            {
                ObjectToShow.Add(obstruction);
            }

            ObjectToHide.Clear();

            // Hide the current obstructions
            foreach (var hit in hits)
            {
                Transform obstruction = hit.transform;
                if (obstruction != player && !obstruction.CompareTag("Yarn") && !obstruction.CompareTag("Enemy") && !obstruction.CompareTag("Meow"))
                {
                    ObjectToHide.Add(obstruction);
                    SetModeTransparent(obstruction);
                }
                ObjectToShow.Remove(obstruction);
            }
        }
        else
        {
            // Mean that no more stuff is blocking the view and sometimes all the stuff is not blocking as the same time
           
            foreach (var obstruction in ObjectToHide)
            {
                ObjectToShow.Add(obstruction);
            }

            ObjectToHide.Clear();

        }
    }

    private void HideObstruction(Transform obj)
    {
        var color = GetRenderer(obj.gameObject).material.color;
        var alpha = obj.CompareTag("CameraWall")? 0.0f: 0.3f;
        color.a = Mathf.Max(alpha, color.a - obstructionFadingSpeed * Time.deltaTime);
        GetRenderer(obj.gameObject).material.color = color;

    }

    private void SetModeTransparent(Transform tr)
    {
        Renderer renderer = GetRenderer(tr.gameObject);
        Material originalMat = renderer.sharedMaterial;
        if (!originalMaterials.ContainsKey(tr))
        {
            originalMaterials.Add(tr, originalMat);
        }
        else
        {
            return;
        }
        Material materialTrans = new Material(transparentMaterial);
        //materialTrans.CopyPropertiesFromMaterial(originalMat);
        renderer.material = materialTrans;
        renderer.material.mainTexture = originalMat.mainTexture;
    }

    private void SetModeOpaque(Transform tr)
    {
        if (originalMaterials.ContainsKey(tr))
        {
            GetRenderer(tr.gameObject).material = originalMaterials[tr];
            originalMaterials.Remove(tr);
        }
    }

    private void ShowObstruction(Transform obj)
    {
        var color = GetRenderer(obj.gameObject).material.color;
        color.a = Mathf.Min(1, color.a + obstructionFadingSpeed * Time.deltaTime);
        GetRenderer(obj.gameObject).material.color = color;
        if (Mathf.Approximately(color.a, 1f))
        {
            SetModeOpaque(obj);
        }
    }

    private Renderer GetRenderer(GameObject referenceObject)
    {
        if(referenceObject.GetComponent<Renderer>() != null)
            return referenceObject.GetComponent<Renderer>();
        return referenceObject.GetComponentInChildren<Renderer>();
        
    }
}
