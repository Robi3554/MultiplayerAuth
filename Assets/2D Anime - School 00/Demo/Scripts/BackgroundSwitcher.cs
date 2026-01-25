using UnityEngine;
using UnityEngine.UI;

public class BackgroundSwitcherUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BG_Storage _database;

    [Header("UI")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _prevButton;

    [Header("Behaviour")]
    [SerializeField] private bool _wrap = true; // true - круговая прокрутка
    [SerializeField] private int _startIndex = 0;

    private int _index;

    private void OnEnable()
    {
        if (_nextButton) _nextButton.onClick.AddListener(Next);
        if (_prevButton) _prevButton.onClick.AddListener(Prev);

        SetIndex(_startIndex, force: true);
        UpdateButtonsInteractable();
    }

    private void OnDisable()
    {
        if (_nextButton) _nextButton.onClick.RemoveListener(Next);
        if (_prevButton) _prevButton.onClick.RemoveListener(Prev);
    }

    public void SetDatabase(BG_Storage database, int startIndex = 0)
    {
        _database = database;
        SetIndex(startIndex, force: true);
        UpdateButtonsInteractable();
    }

    public void Next() => SetIndex(_index + 1);
    public void Prev() => SetIndex(_index - 1);

    public void SetIndex(int index, bool force = false)
    {
        if (_database == null || _database.Count == 0 || _backgroundImage == null) return;

        int newIndex = index;

        if (_wrap)
        {
            newIndex = (newIndex % _database.Count + _database.Count) % _database.Count;
        }
        else
        {
            newIndex = Mathf.Clamp(newIndex, 0, _database.Count - 1);
        }

        if (!force && newIndex == _index) return;

        _index = newIndex;
        _backgroundImage.sprite = _database.Get(_index);

        UpdateButtonsInteractable();
    }

    private void UpdateButtonsInteractable()
    {
        if (!_wrap && _database != null && _database.Count > 0)
        {
            if (_prevButton) _prevButton.interactable = _index > 0;
            if (_nextButton) _nextButton.interactable = _index < _database.Count - 1;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isActiveAndEnabled) SetIndex(_startIndex, force: true);
    }
#endif
}
