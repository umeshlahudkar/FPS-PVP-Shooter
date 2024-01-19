using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private MatchManager matchManager;

    private PlayerController player;

    private void Start()
    {
        if(PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint();
        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation).GetComponent<PlayerController>();
        player.SetReference(this, matchManager);
    }

    public void Die(string damager)
    {
        matchManager.UpdateStatsSendEvent(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if(player != null)
        {
            StartCoroutine(DieCo(damager));
        }
    }

    IEnumerator DieCo(string damager)
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player.gameObject);
        player = null;
        UIController.Instance.ToggleDeathScreen(true, damager);
       
        yield return new WaitForSeconds(respawnTime);

        UIController.Instance.ToggleDeathScreen(false);

        if (matchManager.gameState == GameState.Playing && player == null)
        {
            SpawnPlayer();
        }
    }
}
