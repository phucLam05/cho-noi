using System;
using UnityEngine;
using ChoNoi.Application;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Presentation
{
    public class MarketNewsController : MonoBehaviour
    {
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private MarketNewsDatabaseSO marketNewsDatabase;

        public event Action<MarketNewsEntry> OnNewsChanged;

        public MarketNewsEntry CurrentNews { get; private set; }

        public void Configure(TimeManager timeSource, MarketNewsDatabaseSO database)
        {
            timeManager = timeSource;
            marketNewsDatabase = database;
            RefreshNews();
        }

        private void OnEnable()
        {
            if (timeManager != null)
            {
                timeManager.OnDayChanged += HandleDayChanged;
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.OnDayChanged -= HandleDayChanged;
            }
        }

        private void Start()
        {
            if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
            RefreshNews();
        }

        private void HandleDayChanged(int dayNumber)
        {
            RefreshNews(dayNumber);
        }

        private void RefreshNews(int? dayOverride = null)
        {
            int dayNumber = dayOverride ?? (timeManager != null ? timeManager.CurrentDay : 1);
            CurrentNews = marketNewsDatabase != null ? marketNewsDatabase.GetEntryForDay(dayNumber) : null;
            OnNewsChanged?.Invoke(CurrentNews);
        }
    }
}
