using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Saga
{

	public class SagaSetup : MonoBehaviour
	{
		public Camera theCamera;
		//UI TRINKETS
		public Text difficultyText, initialText, additionalText;
		public Transform heroContainer;
		public Button adaptiveButton, startMissionButton;
		public GameObject miniMugPrefab;
		public Image allyImage;
		public MWheelHandler threatValue, addtlThreatValue;
		public TextMeshProUGUI versionText;
		//UI PANELS
		public SagaAddHeroPanel addHeroPanel;
		public SagaModifyGroupsPanel modifyGroupsPanel;
		//OTHER
		public GameObject warpEffect;
		public Transform thrusterRoot;
		public SagaSetupLanguageController languageController;
		public CanvasGroup faderCG;
		public ColorBlock redBlock;
		public ColorBlock greenBlock;
		public VolumeProfile volume;
		public MissionPicker missionPicker;
		//UI objects using language translations

		Sound sound;
		SagaSetupOptions setupOptions;

		void Awake()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			versionText.text = "App Mission Format: " + Utils.formatVersion;

			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			//bootstrap the setup screen for debugging
			//bootstrapDEBUG();

			//set translated UI
			languageController.SetTranslatedUI();

			//apply settings
			if ( volume.TryGet<Bloom>( out var bloom ) )
				bloom.active = PlayerPrefs.GetInt( "bloom" ) == 1;
			if ( volume.TryGet<Vignette>( out var vig ) )
				vig.active = PlayerPrefs.GetInt( "vignette" ) == 1;

			sound = FindObjectOfType<Sound>();
			sound.CheckAudio();

			setupOptions = new SagaSetupOptions();
			threatValue.ResetWheeler( 3 );
			addtlThreatValue.ResetWheeler();
			DataStore.StartNewSagaSession( setupOptions );
			ResetSetup();

			faderCG.alpha = 0;
			faderCG.DOFade( 1, .5f );
		}

		void bootstrapDEBUG()
		{
			DataStore.InitData();
		}

		/// <summary>
		/// set default mission options, add default ignored groups
		/// </summary>
		public void ResetSetup()
		{
			//difficulty
			difficultyText.text = DataStore.uiLanguage.uiSetup.normal;
			//adaptive
			adaptiveButton.colors = setupOptions.useAdaptiveDifficulty ? greenBlock : redBlock;
			if ( heroContainer.childCount > 0 )
			{
				for ( int i = 1; i < heroContainer.childCount; i++ )
				{
					Destroy( heroContainer.GetChild( i ).gameObject );
				}
			}
			//threat
			initialText.text = setupOptions.threatLevel.ToString();
			additionalText.text = setupOptions.addtlThreat.ToString();
			//clear ignored groups
			DataStore.sagaSessionData.MissionIgnored.Clear();
			//add default ignored
			//ignore "Other" expansion enemy groups by default
			DataStore.sagaSessionData.MissionIgnored.AddRange( DataStore.deploymentCards.Where( x => x.expansion == "Other" ) );
		}

		public void OnCancel()
		{
			thrusterRoot.DOMoveZ( -30, .5f );
			faderCG.DOFade( 0, .5f ).OnComplete( () => SceneManager.LoadScene( "Title" ) );
		}

		public void OnStartMission()
		{
			setupOptions.threatLevel = threatValue.wheelValue;
			setupOptions.addtlThreat = addtlThreatValue.wheelValue;
			setupOptions.projectItem = missionPicker.selectedMission;

			DataStore.sagaSessionData.setupOptions = setupOptions;
			Warp();
		}

		public void AddHero()
		{
			sound.PlaySound( FX.Click );
			EventSystem.current.SetSelectedGameObject( null );
			addHeroPanel.Show( 0, () =>
			 {
				 UpdateHeroes();
			 } );
		}

		public void AddAlly()
		{
			sound.PlaySound( FX.Click );
			EventSystem.current.SetSelectedGameObject( null );
			if ( DataStore.sagaSessionData.selectedAlly == null )
			{
				addHeroPanel.Show( 1, () =>
				{
					UpdateHeroes();
				} );
			}
			else
			{
				DataStore.sagaSessionData.selectedAlly = null;
				allyImage.gameObject.SetActive( false );
			}
		}

		public void OnIgnored()
		{
			sound.PlaySound( FX.Click );
			modifyGroupsPanel.Show( 0 );
		}

		public void OnVillains()
		{
			sound.PlaySound( FX.Click );
			modifyGroupsPanel.Show( 1 );
		}

		public void RemoveHero( DeploymentCard card )
		{
			DataStore.sagaSessionData.MissionHeroes.Remove( card );
			UpdateHeroes();
		}

		void UpdateHeroes()
		{
			for ( int i = 1; i < heroContainer.childCount; i++ )
			{
				Destroy( heroContainer.GetChild( i ).gameObject );
			}
			foreach ( var item in DataStore.sagaSessionData.MissionHeroes )
			{
				var mug = Instantiate( miniMugPrefab, heroContainer );
				mug.transform.GetChild( 0 ).GetComponent<Image>().sprite = Resources.Load<Sprite>( $"Cards/Heroes/{item.id}" );
				mug.GetComponent<MiniMug>().card = item;
			}
			if ( DataStore.sagaSessionData.MissionHeroes.Count > 0 )
				heroContainer.parent.GetChild( 0 ).gameObject.SetActive( false );
			else
				heroContainer.parent.GetChild( 0 ).gameObject.SetActive( true );

			//ally
			if ( DataStore.sagaSessionData.selectedAlly == null )
				allyImage.gameObject.SetActive( false );
			else
			{
				allyImage.gameObject.SetActive( true );
				allyImage.sprite = Resources.Load<Sprite>( $"Cards/Allies/{DataStore.sagaSessionData.selectedAlly.id.Replace( "A", "M" )}" );
			}
		}

		public void OnDifficulty( Button thisButton )
		{
			sound.PlaySound( FX.Click );
			difficultyText.text = setupOptions.ToggleDifficulty();
		}

		public void OnAdaptiveDifficulty( Button thisButton )
		{
			sound.PlaySound( FX.Click );
			setupOptions.useAdaptiveDifficulty = !setupOptions.useAdaptiveDifficulty;
			thisButton.colors = setupOptions.useAdaptiveDifficulty ? greenBlock : redBlock;
		}

		public void Warp()
		{
			sound.PlaySound( FX.Click );
			sound.StopMusic();

			thrusterRoot.DOMoveZ( -30, 2 );

			faderCG.DOFade( 0, .5f ).OnComplete( () =>
			{
				sound.PlaySound( 1 );
				sound.PlaySound( 2 );
				GlowTimer.SetTimer( 1.5f, () => warpEffect.SetActive( true ) );
				GlowTimer.SetTimer( 5, () =>
				{
					DOTween.To( () => theCamera.fieldOfView, x => theCamera.fieldOfView = x, 0, .25f )
					.OnComplete( () =>
					{
						//all effects/music finish, load the mission
						GlowTimer.SetTimer( 3, () =>
						{
							SceneManager.LoadScene( "Saga" );
						} );
					} );
				} );
			} );
		}

		private void Update()
		{
			if ( DataStore.sagaSessionData.MissionHeroes.Count > 0 && missionPicker.selectedMission != null )
				startMissionButton.interactable = true;
			else
				startMissionButton.interactable = false;
		}
	}
}