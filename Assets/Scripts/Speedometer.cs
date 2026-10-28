using UnityEngine;


[RequireComponent(typeof(Rigidbody))]

public class Speedometer : MonoBehaviour
{   
    private Rigidbody _rigidbody;

    public float HorizontalSpeed
    {
        get
        {
            float projected_speed = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, _rigidbody.transform.up).magnitude;
            return Mathf.Round(projected_speed);
        }
    }

    public float Speed
    {
        get
        {
            float speed = _rigidbody.linearVelocity.magnitude;
            
            return Mathf.Round(speed);
        }
    }
}
