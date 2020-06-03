using UnityEngine;

class CollisionMatrix
{
    // TODO: take this from circuit line width?
    private const float STEP_AMOUNT = .0075f;
    private static Transform _mainBoard;
    public static GameObject[,] matrix { get; private set; }

    static CollisionMatrix()
    {
        _mainBoard = GameObject.Find("Main Board").transform;

        mapCollisionMatrix();
    }

    public static void drawRayToCollisionMatrixPoint((int, int) point)
    {
        var renderer = _mainBoard.transform.Find("PCB").GetComponent<Renderer>();
        var x = renderer.bounds.min.x + (point.Item1 * STEP_AMOUNT);
        var y = _mainBoard.position.y + 1f;
        var z = renderer.bounds.min.z + (point.Item2 * STEP_AMOUNT);

        var rayOrigin = new Vector3(x, y, z);
        var rayDirection = Vector3.down;
        Debug.DrawRay(rayOrigin, rayDirection, Color.white, 999999);
    }

    public static (int, int) getPositionInCollisionMatrix(Vector3 transformWorldPosition)
    {
        var renderer = _mainBoard.transform.Find("PCB").GetComponent<Renderer>();

        var xIndex = Mathf.CeilToInt((transformWorldPosition.x - renderer.bounds.min.x) / STEP_AMOUNT);
        var zIndex = Mathf.CeilToInt((transformWorldPosition.z - renderer.bounds.min.z) / STEP_AMOUNT);
        return (xIndex, zIndex);
    }

    public static Vector3 convertGridSpaceToObjectSpace(int xIndex, int yIndex)
    {
        var offsetX = -1 * ((matrix.GetLength(0) / 2) - xIndex);
        var offsetZ = -1 * ((matrix.GetLength(1) / 2) - yIndex);

        return new Vector3(
            offsetFromGridPosition(offsetX),
            .3f,
            offsetFromGridPosition(offsetZ));
    }

    public static bool InBounds(int xIndex, int zIndex)
    {
        return xIndex > 1 && xIndex < matrix.GetLength(0) - 1
               && zIndex > 1 && zIndex < matrix.GetLength(1) - 1;
    }

    static float offsetFromGridPosition(int gridPosition)
    {
        return STEP_AMOUNT * gridPosition
            * 2; // for parent scaling
    }

    private static void mapCollisionMatrix()
    {
        RaycastHit raycastHit;
        var rayDirection = Vector3.down;
        var renderer = _mainBoard.transform.Find("PCB").GetComponent<Renderer>();
        var x = renderer.bounds.min.x;
        var y = _mainBoard.position.y + 1f;
        var z = renderer.bounds.min.z;

        // TODO: Can we just change this to bounds.width?
        int horizontalPositionCount = Mathf.CeilToInt((renderer.bounds.max.x - renderer.bounds.min.x) / STEP_AMOUNT);
        int verticalPositionCount = Mathf.CeilToInt((renderer.bounds.max.z - renderer.bounds.min.z) / STEP_AMOUNT);
        matrix = new GameObject[horizontalPositionCount, verticalPositionCount];

        for (var zIndex = 0; zIndex < verticalPositionCount; zIndex++)
        {
            for (var xIndex = 0; xIndex < horizontalPositionCount; xIndex++)
            {
                var rayOrigin = new Vector3(x, y, z);
                bool isHit = Physics.Raycast(new Ray(rayOrigin, rayDirection), out raycastHit, 2f);

                if (isHit)
                {
                    if (raycastHit.collider.gameObject.name == "PCB")
                    {
                        // Debug.DrawRay(rayOrigin, rayDirection, Color.green, 999999);
                        matrix[xIndex, zIndex] = null;
                    }
                    else
                    {
                        Debug.DrawRay(rayOrigin, rayDirection, Color.red, 999999);
                        matrix[xIndex, zIndex] = raycastHit.collider.gameObject;
                    }
                }

                x += STEP_AMOUNT;
            }
            z += STEP_AMOUNT;
            x = renderer.bounds.min.x;
        }
    }
}
