using System;
using System.Collections.Generic;
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
    [SerializeField] private int _payloadDepth;
    [SerializeField] private double _startIntensity;
    [SerializeField] private bool _showGrid;
    [SerializeField] private int _firePoolSize = 20;
    [SerializeField] private float _updateInterval = 1;
    [SerializeField] private List< Vector2 > _startingFlames;
    [SerializeField] private int _spreadLimit = Int32.MaxValue;
    [SerializeField] private Boolean _loadFromFile = true;
    
    private readonly float _verticalOffset = 0;
    private List< GridItem > _activeFlames;
    private List< GridItem > _edgeFlames;
    private ObjectPoolController _flamePool;
    private GridController _fireGrid;
    private float _tick;
    private FlameController _flamePrefab;
    [SerializeField] private double _spreadChance = 0.3;
    [SerializeField] private double _growthFactor;
    [SerializeField] private double _maxFlameIntensity = 3d;
    [SerializeField] private double _waterStrength = 0.1f;
    private GameMaster _gameMaster;
    [SerializeField] private int _prewarm = 0;
    [SerializeField] private double _spreadIntensity = 3;
    [SerializeField] private string _configFileName = "";

    private void Awake()
    {
        _edgeFlames = new List< GridItem >();
        _activeFlames = new List< GridItem >();
        
        SceneManager.sceneLoaded += Initialize;
        
    }

    private void Initialize( Scene arg0, LoadSceneMode arg1 )
    {
        if( gameObject == null ) return;
        if ( _configFileName == "" ) _configFileName = arg0.name;
        _gameMaster = FindObjectOfType< GameMaster >();
        
        if ( _loadFromFile )
        {
            _gameMaster.LoadFromConfig( _configFileName, this );
        }
        else
        {
            _gameMaster.SaveToConfig( _configFileName, this );
        }
        
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
            gColor[ 1 ].time = 0.8f;
            gColor[ 2 ].color = Color.yellow;
            gColor[ 2 ].time = 0.9f;
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
    }


    // Update is called once per frame
    void Update()
    {
        if( _gameMaster == null || _gameMaster.Paused ) return;
        
        if ( _showGrid )
        {
            _fireGrid.DrawGrid();
            
        }

        if ( _activeFlames.Count == 0 )
        {
            _gameMaster.OnVictory();
        }
        
        if ( Time.time - _tick > _updateInterval && _activeFlames.Count > 0 )
        {
            Spread();
            Grow();
            _tick = Time.time;
        }
        
        
        if ( _showGrid ) _fireGrid.DrawGrid();

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
            foreach ( var neighbour in item.GetNeighbours() )
            {
                if ( !neighbour.GetVariable<bool>( "onfire" ) )
                {
                    add = true;
                }
            }
            if( add ) _edgeFlames.Add( item );
        }
        
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
            
            foreach( GridItem n in edgeItem.GetNeighbours() )
            {
                if ( !n.GetVariable<bool>( "onfire" ) && n.GetVariable<bool>( "flammable" ) )
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
//                    _gameMaster.OnDeath( );
                    return;
                }; 
                spreadCount++;
                var center = _fireGrid.GetPosition( neighbour._gridCoords );
                var position = new Vector3(
                    center.x,
                    _verticalOffset,
                    center.y
                );
//                Debug.Log("Setting new flame to " + neighbour._gridCoords);
//                Debug.Log("To position" +   position);
                newFlame.transform.position = position;
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

    public void SpreadWater( Vector3 position )
    {
        //TODO lower intensity terrain
        //the terrain will spread negative height outwards until it reaches 0
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
//                Debug.Log( "Removing flame" );
                
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

     private void OnDestroy()
    {
        _fireGrid.Dispose();
    }
}