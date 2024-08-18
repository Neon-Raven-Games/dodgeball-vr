using System.Collections;
using System.Collections.Generic;
using Hands.SinglePlayer.EnemyAI.Abilities;
using UnityEngine;

public class SmokePoofOnTrigger : MonoBehaviour
{
    [SerializeField] private GameObject smokePoof;
    [SerializeField] private float delay = 1f;
    [SerializeField] private int numberOfPoofs = 5;
    private List<GameObject> poofs = new();

    private int index;
    // Start is called before the first frame update
    private void Start()
    {
        for (var i = 0; i < numberOfPoofs; i++)
        {
            var poof = Instantiate(smokePoof, Vector3.zero, Quaternion.identity);
            poof.SetActive(false);
            poofs.Add(poof);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BallMovement>())
        {
            if (index >= poofs.Count) index = 0;
            poofs[index].gameObject.SetActive(false);
            poofs[index].transform.position = other.transform.position;
            poofs[index].SetActive(true);
            index++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
