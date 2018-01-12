using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using UnityEngine;

/// <summary>
/// Controls the spread, damage and visualization of the fire.
/// The fire is based on a grid.
/// </summary>
public class FireSystemController : MonoBehaviour
{
    [SerializeField] private int _rows, _columns, _payloadDepth, _startIntensity;
    [SerializeField] private bool _showGrid;
    [SerializeField] private int _firePoolSize = 20;
    [SerializeField] private float _updateInterval = 1;
    [SerializeField] private List< FlameController > _activeFlames;

    private List< GridItem > _edgeFlames;
    private ObjectPoolController _flamePool;
    private GridController _fireGrid;
    private float _tick;

    // Use this for initialization
    void Start()
    {
        _fireGrid = new GridController( _rows, _columns, _payloadDepth, gameObject );
        
        _fireGrid.InitVariable( "intensity", 0 );

        _edgeFlames = new List< GridItem >();
        
        FlameController flamePrefab = Resources.Load< FlameController >( "Prefabs/Flame" );

        _flamePool = GameMaster.InstantiatePool( _firePoolSize, flamePrefab );
        //TODO setup intensity terrain

        foreach ( FlameController flame in _activeFlames )
        {
            var cell = _fireGrid.GetGridItem( flame.transform.position );
            cell.SetPayload( new MonoBehaviour[] {flame} );
            cell.SetVariable( "intensity", _startIntensity );
            _edgeFlames.Add( cell  );
        }
        
        _tick = Time.time;

        if ( _showGrid )
        {
            _fireGrid.DrawGrid();
        }
    }


    // Update is called once per frame
    void Update()
    {
        if ( Time.time - _tick > _updateInterval )
        {
            Spread();
//            Grow();
            _tick = Time.time;
        }

        if ( _showGrid ) _fireGrid.DrawGrid();

        if ( Input.GetMouseButtonDown( 0 ) )
        {
            Debug.Log( _fireGrid.GetCoords( 
                Camera.main.ScreenToWorldPoint(Input.mousePosition) ) );
        }
    }

    private void Grow()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    private void Spread()
    {
        /*
         * TODO Update the intensity terrain 
         * depending on how each flame influences it's neighbouring cells.
         */
        
        /*
         * TODO Remove flame controllers
         * from grid cells if intensity falls below their threshold.
         */

        /*
         * TODO Add new flame controllers
         * to the appropriate grid cells depending
         * on intensity thresholds.
         */
        
        List<GridItem> newEdges = new List<GridItem>();
        foreach ( GridItem edgeItem in _edgeFlames )
        {
            if ( edgeItem.GetVariable<int>( "intensity" ) <= 1 ) continue;
            List< GridItem > emptyNeighbours = new List< GridItem >();
            
            foreach( GridItem n in edgeItem.GetNeighbours() )
            {
                if ( !n.HasActivePayloadElements() )
                {
                    emptyNeighbours.Add( n );
                }
            }

            foreach ( GridItem neighbour in emptyNeighbours )
            {
                var verticalOffset = 0.1f;
                FlameController newFlame = _flamePool.Spawn() as FlameController;
                if ( !newFlame ) return;
                var center = _fireGrid.GetPosition( neighbour._gridCoords );
                var position = new Vector3(
                    center.x,
                    verticalOffset,
                    center.y
                );
                Debug.Log("Setting new flame to " + neighbour._gridCoords);
                Debug.Log("To position" +   position);
                newFlame.transform.position = position;
                _activeFlames.Add(newFlame);
                neighbour.SetPayload( newFlame, 0 );
                neighbour.SetVariable( "intensity", 3 );
                neighbour.SetVariable( "verticalOffset", 0.1f );
                //TODO check if neighbour is actually an edge flame
                newEdges.Add(neighbour);
            }
        }
        _edgeFlames = newEdges;
    }

    public void SpreadWater( Vector3 position )
    {
        //TODO lower intensity terrain
        //the terrain will spread negative height outwards until it reaches 0
    }
}