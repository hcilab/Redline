using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/// <summary>
/// Controls the spread, damage and visualization of the fire.
/// The fire is based on a grid.
/// </summary>
public class FireSystemController : MonoBehaviour
{
    [SerializeField] private int _rows;
    [SerializeField] private int _columns; 
    [SerializeField] private double _startIntensity;
    [SerializeField] private bool _showGrid;
    [SerializeField] private int _firePoolSize = 20;
    [SerializeField] private float _updateInterval = 1;
    [SerializeField] private List< Vector2 > _startingFlames;
    [SerializeField] private int _spreadLimit = Int32.MaxValue;
    [SerializeField] private double _spreadChance = 0.3;
    [SerializeField] private int _spreadDistance;
    [SerializeField] private double _growthFactor;
    [SerializeField] private double _maxFlameIntensity = 3d;
    [SerializeField] private double _waterStrength = 0.1f;
    [SerializeField] private int _prewarm = 0;
    [SerializeField] private double _spreadIntensity = 3;
    [SerializeField] private string _configFileName = "";
    [SerializeField] private int _payloadDepth;
    [SerializeField] private int _levelTime;
    [SerializeField] private LevelManager _levelManager;
    
    private readonly float _verticalOffset = 0;
    private List< GridItem > _activeFlames;
    private List< GridItem > _edgeFlames;
    private ObjectPoolController _flamePool;
    private GridController _fireGrid;
    private float _tick = -1;
    private FlameController _flamePrefab;
    private bool initialized = false;
    [SerializeField] private float _growthFeedback = 50;
    [SerializeField] private double _spreadFeedback = 0.2f;

    public int LevelTime
    {
        get { return _levelTime; }
    }

    public void Initialize()
    {
        _edgeFlames = new List< GridItem >();
        _activeFlames = new List< GridItem >();
             
        _flamePrefab = Resources.Load< FlameController >( "Prefabs/Flame" );
        Vector3 floorSize = gameObject.transform.localScale;
        float height = floorSize.z;
        float width = floorSize.x;
        Vector3 itemSize = new Vector3(width/_columns, 1.1f, height/_rows);

        _flamePrefab.transform.localScale = itemSize;

        
        _flamePool = GameMaster.InstantiatePool( _firePoolSize, _flamePrefab, "FlamePool" );
        
        
        _fireGrid = new GridController( _rows, _columns, _payloadDepth, gameObject );
        
        _fireGrid.InitVariable( "intensity", 0d, item =>
        {
            var g = new Gradient();
            var gColor = new GradientColorKey[4];
            gColor[ 0 ].color = Color.white;
            gColor[ 0 ].time = 0f;
            gColor[ 1 ].color = Color.gray;
            gColor[ 1 ].time = 0.25f;
            gColor[ 2 ].color = Color.yellow;
            gColor[ 2 ].time = 0.5f;
            gColor[ 3 ].color = Color.red;
            gColor[ 3 ].time = 1f;
            var gAlpha = new GradientAlphaKey[1];
            gAlpha[ 0 ].alpha = 1f;
            gAlpha[ 0 ].time = 0f;
            g.SetKeys( gColor, gAlpha );

            var main = item.GetPayload< FlameController >( 0 )
                .GetComponent< ParticleSystem >().main;
                
            main.startColor = g.Evaluate( 
                (float)( item.GetVariable<double>( "intensity" )/( _maxFlameIntensity ) ) 
                );
        } );
        
        _fireGrid.InitVariable<bool>( "flammable", item =>
        {
            Vector3 pos = _fireGrid.GetPosition( item._gridCoords );

            var posLeft = new Vector3(
                pos.x + _fireGrid._itemWidth / 2,
                0.2f,
                pos.y
                );
            
            var posDown = new Vector3(
                pos.x,
                0.2f,
                pos.y + _fireGrid._itemHeight / 2
                );
            
            var rayLeft = new Ray( posLeft, Vector3.left );
            var rayDown = new Ray( posDown, Vector3.back );
            
            Debug.DrawRay( rayLeft.origin, rayLeft.direction * _fireGrid._itemWidth, Color.white, 30000 );
            Debug.DrawRay( rayDown.origin, rayDown.direction * _fireGrid._itemHeight, Color.white, 30000 );

            var flammable = !( Physics.Raycast( rayDown, _fireGrid._itemHeight )
                               || Physics.Raycast( rayLeft, _fireGrid._itemWidth ) );
            return flammable;
        } );
        
        _fireGrid.InitVariable( "onfire", false );
        

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
            var cell = _fireGrid.GetGridItem( coords.x, coords.y );
            cell.SetPayload( flame, 0 );
            cell.SetVariable( "intensity", _startIntensity );
            cell.SetVariable( "onfire", true );
            _activeFlames.Add( cell );
            _edgeFlames.Add( cell  );
            _fireGrid.UpdateGridItem( cell._gridCoords, cell );
        }
        
        _tick = Time.time;

        var orgSpreadChange = _spreadChance;
        _spreadChance = 1f;
        
        for ( int i = 0; i < _prewarm; i++ )
        {
            Spread();
            Grow();
        }
        
        _spreadChance = orgSpreadChange;
        
        _levelManager.GameMaster.ResetUi();
        
        initialized = true;

    }

    private void OnDrawGizmos()
    {
        _fireGrid.DrawGrid();
    }

    // Update is called once per frame
    void Update()
    {
        if ( !initialized ) return; //don't start until initialization is done
        if( _levelManager.GameMaster == null || _levelManager.GameMaster.Paused ) return;
    
        if ( _activeFlames.Count == 0 )
        {
            enabled = false;
            StartCoroutine( 
                _levelManager.GameMaster.GameOver( DataCollectionController.DataType.Victory ) );
        }
        
        if ( Time.time - _tick > _updateInterval && _activeFlames.Count > 0 )
        {
            Spread();
            Grow();
            _tick = Time.time;
        }
    }

    private void Grow()
    {
        foreach ( var activeFlame in _activeFlames )
        {
            if ( activeFlame.GetVariable< double >( "intensity" ) < _maxFlameIntensity )
            {
                activeFlame.SetVariable( "intensity",
                    activeFlame.GetVariable<double>( "intensity" ) + _growthFactor );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void Spread()
    {
        int spreadCount = 0;
        
        _edgeFlames = new List< GridItem >();
        foreach ( var item in _activeFlames )
        {
            bool add = false;
            foreach ( var neighbour in item.GetNeighbours( _spreadDistance ) )
            {
                if ( !neighbour.GetVariable<bool>( "onfire" ) )
                {
                    add = true;
                }
            }
            if( add ) _edgeFlames.Add( item );
            //limiting the number of flames we're even gonna look at for speed sake
            if ( _edgeFlames.Count >= _spreadLimit * 2 ) break; 
        }
        
        //shuffle those suckers
        int next = _edgeFlames.Count;
        while ( next > 1 )
        {
            next--;
            int k = Random.Range( 0, next + 1 );
            GridItem f = _edgeFlames[ k ];
            _edgeFlames[ k ] = _edgeFlames[ next ];
            _edgeFlames[ next ] = f;
        }
        _edgeFlames.Reverse();
        
        foreach ( GridItem edgeItem in _edgeFlames )
        {
            if( edgeItem.GetVariable<double>( "intensity" ) < _spreadIntensity ) continue;
            List< GridItem > emptyNeighbours = new List< GridItem >();
            
            foreach( GridItem n in edgeItem.GetNeighbours( _spreadDistance ) )
            {
                if ( !n.GetVariable<bool>( "onfire" ) && 
                     n.GetVariable<bool>( "flammable") &&
                     HasLineOfSight( n, edgeItem ) )
                {
                    emptyNeighbours.Add( n );
                }
            }

            foreach ( GridItem neighbour in emptyNeighbours )
            {
                if( Random.value <  1 - _spreadChance ) continue;
                if ( spreadCount >= _spreadLimit ) break;
                FlameController newFlame = _flamePool.Spawn() as FlameController;
                if ( !newFlame )
                {
//                    _levelManager.GameMaster.OnDeath( );
                    return;
                }; 
                spreadCount++;
                newFlame.GetComponent< Collider >().enabled = false;
                var center = _fireGrid.GetPosition( neighbour._gridCoords );
                var position = new Vector3(
                    center.x,
                    _verticalOffset,
                    center.y
                );
//                Debug.Log("Setting new flame to " + neighbour._gridCoords);
//                Debug.Log("To position" +   position);
                newFlame.transform.position = position;
                newFlame.GetComponent< Collider >().enabled = true;
                
                neighbour.SetPayload( newFlame, 0 );
                neighbour.SetVariable( "intensity", 1d );
                neighbour.SetVariable( "onfire", true );
                neighbour.SetVariable( "verticalOffset", _verticalOffset );
                _fireGrid.UpdateGridItem( neighbour._gridCoords, neighbour );
                //TODO check if neighbour is actually an edge flame
                _activeFlames.Add(neighbour);
            }
        }
    }

    public double GetFlameIntensity( FlameController flame )
    {
        foreach ( var activeFlame in _activeFlames )
        {
            if ( activeFlame.GetPayload( 0 ) == flame ) return activeFlame.GetVariable< double >( "intensity" );
        }
        return 0f;
    }

    public FlameController LowerIntensity( double waterStrength, out double outIntensity )
    {
        return LowerIntensity( _fireGrid.GetMouseCoords( ), waterStrength, out outIntensity );

    }

    public FlameController LowerIntensity( Vector3 worldCoords, double particleCount, out double outIntensity )
    {
        return LowerIntensity( _fireGrid.GetWorldCoords( worldCoords ), 
            particleCount * _waterStrength, out outIntensity );
    }

    public FlameController LowerIntensity( Vector2 coords, double waterStrength, out double outIntensity )
    {
        outIntensity = 0f;
        
        GridItem cell = _fireGrid.GetGridItem( coords.x, coords.y );
        
        FlameController flame = cell.GetPayload( 0 ) as FlameController;
//        Debug.Log("Clicked on  " + flame + " at position " + coords);
        if ( flame != null )
        {
            outIntensity = cell.GetVariable< double >( "intensity" );
            cell.SetVariable( "intensity",
                outIntensity - waterStrength
            );
            if ( cell.GetVariable<double>( "intensity" ) <= 0 )
            {
                Debug.Log( _levelManager.Player.GetRemainingHitPoints() / _growthFeedback );
                if ( _activeFlames.Count < _levelManager.Player.GetRemainingHitPoints() / _growthFeedback )
                    Grow();
                if( Random.Range( 0f, 1f ) > _spreadFeedback )
                    Spread();
                _activeFlames.Remove( cell );
                _flamePool.Remove( flame );
                
                cell.SetVariable( "intensity", 0d );
                cell.SetVariable( "onfire", false );
//                Debug.Log( cell.GetVariable<int>( "intensity" )  );
                cell.RemovePayload( 0 );
            } else cell.SetPayload( flame, 0 );
            _fireGrid.UpdateGridItem( coords, cell);
        }
        return flame;
    }

    private void OnDisable()
    {
        initialized = false;
    }

    private void OnDestroy()
    {
        _fireGrid.Dispose();
    }

    public int GetActiveFlames()
    {
        if ( _activeFlames == null ) return 0;
        return _activeFlames.Count;
    }

    public int GetTotalFlames()
    {
        return _flamePool.Count();
    }
    
    public bool HasLineOfSight( GridItem referenceItem, GridItem targetItem )
    {
        Vector2 f1 = _fireGrid.GetPosition( referenceItem._gridCoords );
        Vector2 f2 = _fireGrid.GetPosition( targetItem._gridCoords );
        
        Vector3 flame1 = new Vector3( f1.x, 1, f1.y );
        Vector3 flame2 = new Vector3( f2.x, 1, f2.y );
        
        var distance = Vector3.Distance( flame1, flame2 );
        Ray ray = new Ray(flame2, Vector3.Normalize(flame1 - flame2) * distance);
        var hit = Physics.Raycast( ray, distance );
        if( hit )
            Debug.DrawRay( ray.origin, ray.direction * distance, Color.red, 10000  );
        else
        {
            Debug.DrawRay( ray.origin, ray.direction * distance, Color.green, 3000 );
        }
        return !hit;
    }
}