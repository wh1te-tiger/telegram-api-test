using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class MainScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text Score;
    [SerializeField] private TMP_Text Energy;
    [SerializeField] private TMP_Text Boosters;
    [SerializeField] private Button ClickButton;
    
    private PlayerState _playerState;
    
    private readonly CompositeDisposable _disposables = new();

    public void Initialize(PlayerState playerState)
    {
        _playerState = playerState;

        ClickButton.onClick.AddListener(IncreaseScore);
        
        _playerState.ObserveEveryValueChanged(_ => _playerState.Energy).Subscribe(OnEnergyChanged).AddTo(_disposables);
        _playerState.ObserveEveryValueChanged(_ => _playerState.Score).Subscribe(OnScoreChanged).AddTo(_disposables);
    }
    
    private void OnScoreChanged(int v)
    {
        Score.text = v.ToString();
    }

    private void OnEnergyChanged(int v)
    {
        Energy.text = v.ToString();
        ClickButton.enabled = v >= 0;
    }

    private void IncreaseScore()
    {
        _playerState.IncreaseScore(1);
    }
}
