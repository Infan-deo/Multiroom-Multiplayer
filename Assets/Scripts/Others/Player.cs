using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    [SerializeField] private AnimationClip defaultAnimation;
    [SerializeField] private AnimationClip idleAnimation;
    [SerializeField] private AnimationClip walkAnimation;
    private NavMeshAgent _navMeshAgent;
    public Animator animator;

    private bool _isInteracting;
    private AnimationSystem _animationSystem;
    private IAnimationInteractable _currentInteractable;
    private readonly List<IAnimationInteractable> _interactables = new();
    [SerializeField] private float radius;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _animationSystem = new(animator, idleAnimation, walkAnimation);
    }

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetMouseButtonDown(0))
        // {
        //     if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        //     {
        //         _navMeshAgent.SetDestination(hit.point);
        //     }
        // }
        //
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     
        //     if (_interactables.Count > 0)
        //     {
        //         Accept(_interactables[0]);
        //         _interactables[0].Interact(this);
        //     }
        //     else
        //         print("interactable list is also  null ");
        // }

        _animationSystem.UpdateLocomotion(_navMeshAgent.velocity, _navMeshAgent.speed);
    }

    public void Accept(IAnimationInteractable interactable)
    {
        if (_isInteracting) return;

        _isInteracting = true;
        if (interactable != null)
        {
            _animationSystem.PlayOneShot(interactable.GetAnimationClip());
        }
        else
        {
            print("interactable is  null ");
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered: " + other.name);
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out IAnimationInteractable currentInteractable))
            {
                // Use interactable
                _interactables.Add(currentInteractable);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // if (other.TryGetComponent<IAnimationInteractable>(out _))
        //     _currentInteractable = null;
    }


    private void OnDestroy()
    {
        _animationSystem.Destroy();
    }

    public void SetInteractable(bool b)
    {
        _isInteracting = false;
    }
}