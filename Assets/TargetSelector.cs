using UnityEngine;
using UnityEngine.Events;

public class TargetSelector : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private Camera selectionCamera;
    [SerializeField] private LayerMask targetLayerMask = ~0;
    [SerializeField] private float maxSelectionDistance = 100f;
    
    [Header("Events")]
    public UnityEvent<Transform> OnTargetSelected;
    public UnityEvent OnTargetDeselected;
    
    private Transform currentSelectedTarget;
    
    private void Start()
    {
        if (selectionCamera == null)
            selectionCamera = Camera.main;
    }
    
    private void Update()
    {
        HandleSelectionInput();
    }
    
    private void HandleSelectionInput()
    {
        // Клик левой кнопкой мыши
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = selectionCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, maxSelectionDistance, targetLayerMask))
            {
                SelectTarget(hit.transform);
            }
            else
            {
                DeselectTarget();
            }
        }
        
        // Клик правой кнопкой для сброса цели
        if (Input.GetMouseButtonDown(1))
        {
            DeselectTarget();
        }
    }
    
    private void SelectTarget(Transform target)
    {
        // Проверяем, есть ли у объекта компонент Target
        if (target.GetComponent<Target>() == null)
        {
            Debug.LogWarning($"Selected object {target.name} does not have Target component!");
            return;
        }
        
        currentSelectedTarget = target;
        OnTargetSelected?.Invoke(currentSelectedTarget);
        
        Debug.Log($"<color=green>Target selected: {target.name}</color>");
    }
    
    private void DeselectTarget()
    {
        if (currentSelectedTarget != null)
        {
            currentSelectedTarget = null;
            OnTargetDeselected?.Invoke();
            Debug.Log("<color=yellow>Target deselected</color>");
        }
    }
    
    public Transform GetCurrentTarget() => currentSelectedTarget;
    public bool HasTarget() => currentSelectedTarget != null;
}