﻿using UnityEngine;
using Controllers;

namespace Utils
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSwapper : MonoBehaviour
    {
        private SpriteRenderer spriteRender;
        [SerializeField] private Sprite lightSprite;
        [SerializeField] private Sprite darkSprite;
        private GameController gameController;
        private void Awake()
        {
            gameController = FindObjectOfType<GameController>();
            if (gameController != null)
            {
                gameController.timer.OnPhaseChange.AddListener(SetPhaseMode);
                gameController.timer.OnTimerStart.AddListener(SetPhaseMode);
            }

            spriteRender = GetComponent<SpriteRenderer>();
            SetPhaseMode(gameController.timer.GetWorldPhase());
        }

        private void SetPhaseMode(EWorldPhase worldPhase)
        {
            switch (worldPhase)
            {
                case EWorldPhase.LIGHT:
                    if (lightSprite != null)
                        spriteRender.sprite = lightSprite;
                    break;
                case EWorldPhase.DARK:
                    if (darkSprite != null)
                        spriteRender.sprite = darkSprite;
                    break;
            }
        }

        private void OnDestroy()
        {
            gameController.timer.OnPhaseChange.RemoveListener(SetPhaseMode);
            gameController.timer.OnTimerStart.RemoveListener(SetPhaseMode);
        }
    }
}