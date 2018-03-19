using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DataCollectionController : MonoBehaviour
{
    [SerializeField] private string _serverAddress;
    [SerializeField] private bool _sendRemote;
    private string _dataFile;

    private void Awake()
    {
        FindObjectOfType<GameMaster>().LoadFromConfig( "data_service", this );
        InitDatafile(  );
    }

    public void Submit( string data )
    {
        WWWForm dataObj = new WWWForm();
        dataObj.AddField( "data", data );
        Submit( dataObj );
    }
    
    public void Submit( WWWForm dataObj )
    {
        if ( _sendRemote )
        {
            StartCoroutine( Upload( dataObj ) );
        }
    }

    IEnumerator Upload( WWWForm dataObj )
    {
        UnityWebRequest req = UnityWebRequest.Post( _serverAddress, dataObj );
        yield return req.Send();
    }

    private void InitDatafile( string fileName = "redline_data.csv" )
    {
        var headerString = Encoding.ASCII.GetBytes( 
            "time, id, level, hp, bar_type, damage, score, proximity, active\n" );
        _dataFile = Path.Combine( Application.streamingAssetsPath, Path.Combine( "data", fileName ) );
        if ( !File.Exists( _dataFile ) )
        {
            FileStream fs = File.Create( _dataFile );
            if ( fs.CanWrite )
            {
                fs.Write( headerString, 0, headerString.Length );
                fs.Close();
            }
        }
    }

    public void LogData( 
        float time
        , int sessionId
        , string level
        , string bar_type
        , double hitPoints
        , double damage
        , double score
        , int flamesNearBy
        , int activeFlames )
    {
        if( !File.Exists( _dataFile ) )
        {
            InitDatafile();
        }
        
        var dataString = string.Format( "{0},{1},{2},{3},{4},{5},{6},{7}, {8}\n", 
            time, sessionId, level, hitPoints, bar_type, damage,
            score, flamesNearBy, activeFlames );
        File.AppendAllText( _dataFile, dataString );
        
        WWWForm dataObj = new WWWForm();
        dataObj.AddField( "time", time.ToString());
        dataObj.AddField( "id", sessionId );
        dataObj.AddField( "level", level );
        dataObj.AddField( "hp", hitPoints.ToString() );
        dataObj.AddField( "bar", bar_type );
        dataObj.AddField( "damage", damage.ToString() );
        dataObj.AddField( "score", score.ToString() );
        dataObj.AddField( "proximity", flamesNearBy );
        dataObj.AddField( "active", activeFlames );
        Submit( dataObj );
    }
}