﻿using System;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Representation of a grid in the gameworld.
/// Manages the grid and it's associated objects.
/// </summary>
public class GridController
{	
	private GameObject _gameSpace;
	public readonly int _rows, _cols, _payloadDepth;
	private GridItem[] _grid;
	public readonly float _spaceWidth, _spaceHeight, _itemWidth, _itemHeight;

	private LineRenderer _lineRenderer;
	private bool _gridCanUpdate = false;

	/// <summary>
	/// Creates a new grid. This grid can hold <i>rows</i> by <i>cols</i>
	/// cells. Each cells can have <i>payloadDepth</i> number of items associated 
	/// with it as well as generic information. 
	/// </summary>
	/// <param name="rows">Number of rows</param>
	/// <param name="cols">Number of columns</param>
	/// <param name="payloadDepth">Number of MonoBehaviour objets to be attached to
	/// each grid cell.</param>
	/// <param name="space">The game space that the grid will stretch over 
	/// i.e. a 3D plane).</param>
	public GridController(int rows, int cols, int payloadDepth, GameObject space)
	{
		_rows = rows;
		_cols = cols;
		_payloadDepth = payloadDepth;
		_gameSpace = space;

		//TODO this will change depending on where UP is... so that's a problem
		_spaceWidth = _gameSpace.GetComponent<Renderer>().bounds.size.x;
		_spaceHeight = _gameSpace.GetComponent<Renderer>().bounds.size.z;

		_itemWidth = _spaceWidth / _cols;
		_itemHeight = _spaceHeight / _rows;
		
		_grid = new GridItem[_cols*_rows];

		for (int i = 0; i < _cols * _rows; i++)
		{
			_grid[ i ] = new GridItem( 
				new MonoBehaviour[_payloadDepth]
				, this
				, new Vector2(i/_rows, i%_rows)
			);
		}
	}

	public GridController( int rows, int cols, MonoBehaviour[] payload, GameObject space )
	: this( rows, cols, payload.Length, space) 
	{
		var fogGroup = new GameObject(space.name + "Pool");
		foreach ( GridItem item in _grid )
		{
			foreach ( var monoBehaviour in payload )
			{
				MonoBehaviour payloadItem = MonoBehaviour.Instantiate( monoBehaviour );
				payloadItem.transform.SetParent( fogGroup.transform );
				var pos = GetPosition( item._gridCoords );
				payloadItem.transform.position = new Vector3(
					pos.x,
					1,
					pos.y);
				
				payloadItem.transform.localScale = new Vector3(
					_itemWidth,
					_itemHeight,
					1f
					);
				
				item.SetPayload( payloadItem );
			}
		}
	}

	/// <summary>
	/// Retruns the coordinates of a position vector relative to the game world. 
	/// </summary>
	/// <param name="pointOfInterest">Position of the point of interest relative to the 
	/// whole game world.</param>
	/// <returns>A 2 item vector with the grid row and column of the point of interest.</returns>
	/// <exception cref="IndexOutOfRangeException">When point requested is out of the bounds of
	/// the grid object.</exception>
	public Vector2 GetMouseCoords()
	{
		var ray = Camera.main.ScreenPointToRay(
			Input.mousePosition );
		return GetRayCoords( ray );
	}

	public Vector2 GetWorldCoords( Vector3 position )
	{
		position.y = -1;
		var ray = new Ray( position, Vector3.up );
		return GetRayCoords( ray );
	}

	private Vector2 GetRayCoords( Ray ray )
	{
		var spaceCollider = _gameSpace.GetComponent<Collider>();
		RaycastHit hit;
		Vector3 point;
		
//		Debug.DrawRay(ray.origin, ray.direction, Color.green, 20, false);
		
		if( spaceCollider.Raycast( ray, out hit, 1000) )
			point = hit.point;
		else
			throw new IndexOutOfRangeException("Point " + Input.mousePosition + "is not within the grid.");

		point = _gameSpace.transform.InverseTransformPoint(point);
		
		point += new Vector3(5, 0, -5);
		point.z *= -1;
		
		return new Vector2( 
			(int) (point.x / ( 10f / _cols ) )
			, 	(int) (point.z / ( 10f / _rows ) ));
	}

	/// <summary>
	/// Assigns a new GridItem to a cell in the grid.
	/// </summary>
	/// <param name="coords">Vector of the grid coordinates of the cell to be updated.</param>
	/// <param name="item">Item to be associated with the cell.</param>
	public void UpdateGridItem(Vector2 coords, GridItem item)
	{
		UpdateGridItem((int) coords.x, (int) coords.y, item);
	}

	/// <summary>
	/// Assigns a new GridItem to a cell in the grid.
	/// </summary>
	/// <param name="x">Row of the cell to be updated.</param>
	/// <param name="y">Column of the cell to be updated.</param>
	/// <param name="item">Item to be associated with the cell.</param>
	public void UpdateGridItem(int x, int y, GridItem item)
	{
		_grid[x * _rows + y] = item;
	}
	
	/// <summary>
	/// Retrieves the grid item in a certain grid cell.
	/// </summary>
	/// <param name="x">Row of the grid item.</param>
	/// <param name="y">Column of the grid item.</param>
	/// <returns>A GridItem that is associated with the specified cell.</returns>
	public GridItem GetGridItem(int x, int y)
	{
		try
		{
			return _grid[ x * _rows + y ];
		}
		catch ( Exception e )
		{
			Debug.Log( "X: " + x + " Y: " + y  );
			throw e;
		}
	}
	public GridItem GetGridItem(double x, double y)
	{
		return GetGridItem((int) x, (int) y);
	}
	
	/// <summary>
	/// Draws the mathematical grid on the game object.
	/// </summary>
	public void DrawGrid()
	{
		_lineRenderer = _gameSpace.GetComponent<LineRenderer>();
		if( !_lineRenderer )
			_lineRenderer = _gameSpace.AddComponent<LineRenderer>();
		
		_lineRenderer.startWidth = 0.1f;
		_lineRenderer.endWidth = 0.1f;
		/*
		 * Since the line has to be continuous we have to draw a snake line thingy to make
		 * the grid
		 *
		 */ 
		
		_lineRenderer.positionCount = ( _rows * 2 + 1 ) 
		                              + ( _cols * 2 + 1 ) + 2;
		_lineRenderer.useWorldSpace = false;
		
		float x = -5;
		float y = 0.1f;
		float z = -5;

		Vector3[] positions = new Vector3[_lineRenderer.positionCount];
		Vector3 position;
		int i;
		for (i = 0; i < _rows * 2 + 1; i++)
		{
			position = new Vector3(x,y,z);
			positions[i] = position;

			//Lines going down, right, up, right .. repeat
			switch ( i % 2 )
			{
				case 0:
					z *= -1;
					break;
				case 1:
					x += 10f/_rows;
					break;
			}
		}
		
		//determine if we have to draw a vertical edge on the right
		if ( _rows % 2 != 0 )
		{
			z *= -1; 
			positions[i] = new Vector3(x, y, z);
		}

		for (; i - _rows * 2 + 1 < _cols * 2 + 1; i++)
		{
			//lines going left, up, right up, repeat
			switch (i % 2)
			{
				case 0:
					x *= -1;
					break;
				case 1:
					z -= 10f / _cols;
					break;
			}
			
			positions[i] = new Vector3(x, y , z);
		}
		
		//Draw a final outline
		positions[i++] = new Vector3(-5, y, -5);
		positions[i++] = new Vector3(-5, y, 5);
		positions[i++] = new Vector3(5, y, 5);
		positions[i] = new Vector3(5, y, -5);
		_lineRenderer.SetPositions(positions);
	}

	public void InitVariable<T>( string name, T value)
	{
		foreach ( GridItem n in _grid )
		{
			n.SetVariable<T>( name, value );
		}
	}
	
	public void InitVariable<T>( string name, T value, GridItem.VariableEvent variableEvent )
	{
		foreach ( var item in _grid )
		{
			item.SetVariable< T >( name, value );
			item.AttachVariableEvent( name, variableEvent );
		}
	}
	
	public void InitVariable< T >( string name, GridItem.VariableSetter setter)
	{
		foreach ( GridItem gridItem in _grid )
		{
			gridItem.SetVariable( name, setter );
			_gridCanUpdate = true;
			gridItem.SetVariable( "canUpdate", true );
		}
	}

	public void UpdateVariable<T>( String key )
	{
		if ( _gridCanUpdate )
		{
			foreach ( var gridItem in _grid )
			{
				gridItem.UpdateVariable( key );
			}	
		}
	}

	public Vector2 GetPosition(Vector2 coords)
	{
		return new Vector2(
			coords.x * _itemWidth + (_itemWidth/2) - _spaceWidth/2 - _gameSpace.transform.position.x,
			coords.y * -_itemHeight - (_itemHeight/2) + _spaceHeight/2 +_gameSpace.transform.position.z
			);
	}

	public void Dispose()
	{
		foreach ( var gridItem in _grid )
		{
			gridItem.Dispose();
		}
	}
}
