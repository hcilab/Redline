using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    [SerializeField] private List< Vector2 > _startingFlames;
    
    private readonly float _verticalOffset = .2f;
    private List< GridItem > _activeFlames;
    private List< GridItem > _edgeFlames;
    private ObjectPoolController _flamePool;
    private GridController _fireGrid;
    private float _tick;
    private FlameController _flamePrefab;

    private void Awake()
    {
        _edgeFlames = new List< GridItem >();
        _activeFlames = new List< GridItem >();
     
        _flamePrefab = Resources.Load< FlameController >( "Prefabs/LameFlame" );
        
        Vector3 floorSize = gameObject.transform.localScale;
        float height = floorSize.z;
        float width = floorSize.x;
        Vector3 itemSize = new Vector3(width/_columns, 1.1f, height/_rows);

        _flamePrefab.transform.localScale = itemSize;
        
        _flamePool = GameMaster.InstantiatePool( _firePoolSize, _flamePrefab );
        
        _fireGrid = new GridController( _rows, _columns, _payloadDepth, gameObject );
        
        _fireGrid.InitVariable( "intensity", 0 );
        _fireGrid.InitVariable( "onfire", false );
        //TODO setup intensity terrain

        foreach ( Vector2 coords in _startingFlames )
        {
            FlameController flame = _flamePool.Spawn() as FlameController;
            if ( !flame ) throw new Exception("Can't start a fire! Someone bring more matches!!");
            var gridCoords = _fireGrid.GetPosition( coords );
            flame.transform.position = new Vector3(
                gridCoords.x,
                _verticalOffset,
                gridCoords.y
                );
            flame.SetIntensity( 3f );
            var cell = _fireGrid.GetGridItem( coords.x, coords.y );
            cell.SetPayload( flame, 0 );
            cell.SetVariable( "intensity", _startIntensity );
            cell.SetVariable( "onfire", true );
            _activeFlames.Add( cell );
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
        
        
        foreach ( GridItem edgeFlame in _edgeFlames )
        {
            (edgeFlame.GetPayload( 0 ) as FlameController)
                .gameObject
                .GetComponentInChildren< EnemyController >()
                .setActive();
        }

        if ( _showGrid ) _fireGrid.DrawGrid();

        if ( Input.GetMouseButtonDown( 0 ) )
        {
         
            Debug.Log( _fireGrid.GetMouseCoords( ) );
            
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
        foreach ( var edgeItem in _edgeFlames )
        {
            (edgeItem.GetPayload( 0 ) as FlameController)
                .gameObject
                .GetComponentInChildren< EnemyController >()
                .setInactive();
        }
        _edgeFlames = new List< GridItem >();
        foreach ( var item in _activeFlames )
        {
            bool add = false;
            foreach ( var neighbour in item.GetNeighbours() )
            {
                if ( !neighbour.GetVariable<bool>( "onfire" ) )
                {
                    add = true;
                }
            }
            if( add ) _edgeFlames.Add( item );
        }
        
        foreach ( GridItem edgeItem in _edgeFlames )
        {
            List< GridItem > emptyNeighbours = new List< GridItem >();
            
            foreach( GridItem n in edgeItem.GetNeighbours() )
            {
                if ( !n.GetVariable<bool>( "onfire" ) )
                {
                    emptyNeighbours.Add( n );
                }
            }

            foreach ( GridItem neighbour in emptyNeighbours )
            {
                FlameController newFlame = _flamePool.Spawn() as FlameController;
                if ( !newFlame ) return; 
                var center = _fireGrid.GetPosition( neighbour._gridCoords );
                var position = new Vector3(
                    center.x,
                    _verticalOffset,
                    center.y
                );
//                Debug.Log("Setting new flame to " + neighbour._gridCoords);
//                Debug.Log("To position" +   position);
                newFlame.transform.position = position;
                newFlame.SetIntensity( 3f );
                neighbour.SetPayload( newFlame, 0 );
                neighbour.SetVariable( "intensity", 3 );
                neighbour.SetVariable( "onfire", true );
                neighbour.SetVariable( "verticalOffset", _verticalOffset );
                //TODO check if neighbour is actually an edge flame
                _activeFlames.Add(neighbour);
            }
        }
    }

    public void SpreadWater( Vector3 position )
    {
        //TODO lower intensity terrain
        //the terrain will spread negative height outwards until it reaches 0
    }

    public void LowerIntensity( float waterStrength )
    {
        Vector2 coords = _fireGrid.GetMouseCoords( );
        GridItem cell = _fireGrid.GetGridItem( coords.x, coords.y );

        FlameController flame = cell.GetPayload( 0 ) as FlameController;
        Debug.Log("Clicked on  " + flame + " at position " + coords);
        if ( flame != null )
        {
            flame.SetIntensity( flame.GetIntensity() - waterStrength );
    
            if ( flame.GetIntensity() <= 0 )
            {
                Debug.Log( "Removing flame" );
                
                _activeFlames.Remove( cell );
                _flamePool.Remove( flame );
                
                cell.SetVariable( "intensity", 0 );
                cell.SetVariable( "onfire", false );
                Debug.Log( cell.GetVariable<int>( "intensity" )  );
                cell.RemovePayload( 0 );
            }
            
            cell.SetPayload( flame, 0 );
            _fireGrid.UpdateGridItem( coords, cell);
        }
    }

    public double GetFlameIntensity( FlameController flame )
    {
        foreach ( var activeFlame in _activeFlames )
        {
            if ( activeFlame.GetPayload( 0 ) == flame ) return activeFlame.GetVariable< double >( "intensity" );
        }
        throw new Exception("Flame cell not found!");
    }
}