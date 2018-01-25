using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// A single square (or whatever shape) in a grid as defined by the GridController.
/// 
/// GridItems can hold a dictionary of generic variables
/// that can be attached to the GridItems.
/// 
/// GridItems can hold an array of MonoBehaviour objects.
/// </summary>
public class GridItem
{
    public delegate void VariableEvent(GridItem item);
    private Dictionary< String, object > _variables;
    private MonoBehaviour[] _payload;
    private int _payloadActiveElementCount = 0;
    private readonly GridController _parentGrid;
    public readonly Vector2 _gridCoords;
    private readonly Vector2 _size;
    private Dictionary<string, VariableEvent> _variableEvents;

    /// <summary>
    /// Creates a new grid item and sets it's payload of MonoBehaviour items.
    /// </summary>
    /// <param name="payload">Array of monobehaviour items.</param>
    /// <param name="size">Size of the grid item in absolute, world space.</param>
    /// <param name="height">Height offset of the gridItems game objects.</param>
    /// <param name="parent">Grid that the grid item belongs to.</param>
    /// <param name="coords">Coordinates of the grid item on the grid.</param>
    public GridItem( 
        MonoBehaviour[] payload
        , GridController parent
        , Vector2 coords
        , Vector2 size 
        ) : this(parent, coords, size)
    {
        _payload = payload;
    }

    /// <summary>
    /// Creates a new grid item.
    /// </summary>
    public GridItem( 
        GridController parent
        , Vector2 coords
        , Vector2 size
        )
    {
        _variableEvents = new Dictionary< string, VariableEvent >();
        _size = size;
        _gridCoords = coords;
        _parentGrid = parent;
        _variables = new Dictionary< string, object >();
    }

    /// <summary>
    /// Retrieves a variable associated with the grid item.
    /// </summary>
    /// <param name="key">Name of the variable.</param>
    /// <returns>The generic value assocaited with the variable.</returns>
    public object GetVariable( String key )
    {
        return _variables[ key ];
    }

    public T GetVariable< T >( String key )
    {
        return ( T ) _variables[ key ];
    }

    /// <summary>
    /// Retrieves the payload associated with a grid item.
    /// </summary>
    /// <returns>An array of MonoBehaviour items.</returns>
    public MonoBehaviour[] GetPayload()
    {
        return _payload;
    }

    /// <summary>
    /// Retrives a specific MonoBehaviour object in the payload.
    /// </summary>
    /// <param name="i">Index of the desired item.</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    /// <returns>The specific MonoBehaviour item.</returns>
    public MonoBehaviour GetPayload( int i )
    {
        return _payload[ i ];
    }

    public T GetPayload< T >( int i ) where T : MonoBehaviour
    {
        return ( T ) _payload[ i ];
    }

    /// <summary>
    /// Sets the payload of a grid item.
    /// If the item has a previous payload associated with it this will clear it.
    /// </summary>
    /// <param name="payload">Array of MonoBehaviour objects to be associated 
    /// with the grid item.</param>
    public void SetPayload( MonoBehaviour[] payload )
    {
        _payload = null;
        _payload = payload;
        foreach( var item in _payload )
            if ( item ) _payloadActiveElementCount++;
    }

    /// <summary>
    /// Sets a specifc payload item in the existing payload of the grid item.
    /// 
    /// Requries that the payload is initialized properly.
    /// </summary>
    /// <param name="payloadItem">MonoBehaviour item to be inserted into the payload.</param>
    /// <param name="i">Index where the new item is to be inserted.</param>
    /// <exception cref="Exception">Thrown if the payload is not intialized</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown if the desired index is out 
    /// of the bounds of the existing payload array. If this occurs consider
    /// recreating an extended version of the payload via SetPayload.</exception>
    public void SetPayload( MonoBehaviour payloadItem, int i )
    {
        if ( _payload == null )
        {
            _payload = new MonoBehaviour[i+1];
            _payload[ i ] = payloadItem;
            _payloadActiveElementCount = 1;
        }
        if ( _payload.Length > i )
        {
            if ( !_payload[ i ] ) _payloadActiveElementCount++;
            _payload[ i ] = payloadItem;
        }
        else throw new IndexOutOfRangeException();
//        Debug.Log("Updated payload for " + this);
    }

    /// <summary>
    /// Removes a payload item from the cell.
    /// </summary>
    /// <param name="i">Index of the payload item to be removed.</param>
    public void RemovePayload( int i )
    {
        if ( i < _payload.Length && _payloadActiveElementCount > 0 )
        {
            _payloadActiveElementCount-=1;
            _payload[ i ] = null;
        }
    }

    /// <summary>
    /// Sets a variable to be associated with the grid item.
    /// </summary>
    /// <param name="key">Name of the variable.</param>
    /// <param name="value">Generic value of the variable.</param>
    /// <returns>Returns true if the variable was successfully inserted.</returns>
    public bool SetVariable< T >( String key, T value )
    {
        if (_variables.ContainsKey(key))
            _variables[key] = value;
        else
            _variables.Add( key, value );

        if ( _variableEvents.ContainsKey( key ) ) _variableEvents[ key ]( this );
        
        return _variables.ContainsKey( key );
    }

    public GridItem[] GetNeighbours()
    {
        List<GridItem> neighbours = new List<GridItem>();
        float x = _gridCoords.x;
        float y = _gridCoords.y;
        
        //Check left
        if (_gridCoords.y > 0)
            neighbours.Add( _parentGrid.GetGridItem( x, y - 1 ) );
        
        //check up
        if (_gridCoords.x > 0)
            neighbours.Add( _parentGrid.GetGridItem( x - 1, y ) );
        
        //check right
        if (_gridCoords.y < _parentGrid._cols - 1) 
            neighbours.Add( _parentGrid.GetGridItem( x, y + 1 ) );
        
        //check down
        if( _gridCoords.x < _parentGrid._rows - 1)
            neighbours.Add( _parentGrid.GetGridItem( x + 1,y ) );

        return neighbours.ToArray();
    }

    public bool HasActivePayloadElements()
    {
        return _payloadActiveElementCount != 0;
    }
    
    public void AttachVariableEvent( string variable,  VariableEvent variableEvent )
    {
        _variableEvents.Add( variable, variableEvent );
    }

    public void Dispose()
    {
        foreach ( var monoBehaviour in _payload )
        {
            Object.Destroy( monoBehaviour );
        }
    }
}