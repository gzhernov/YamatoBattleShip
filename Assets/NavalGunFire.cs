using UnityEngine;
using UnityEngine;

public class NavalGunFire : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem coreFlash;    // Мгновенная вспышка
    public ParticleSystem fireball;     // Огненный шар
    public ParticleSystem gunSmoke;     // Черный дым

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gunBlast;           // Низкочастотный "БУМ"
    
    [Header("Camera Shake")]
    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.7f;

    [Header("Timing")]
    public float rechargeTime = 4f;      // Время перезарядки ГК
    private float nextShotTime = 0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextShotTime)
        {
            FireGun();
            nextShotTime = Time.time + rechargeTime;
        }
    }

    void FireGun()
    {
        // 1. Запуск всех систем в правильном порядке
        if (coreFlash != null) coreFlash.Play();
        if (fireball != null) fireball.Play();
        if (gunSmoke != null) gunSmoke.Play();

        // 2. Звук (очень громкий, с низкими частотами)
        if (audioSource != null && gunBlast != null)
        {
            audioSource.PlayOneShot(gunBlast, 1.0f);
        }

        // 3. Тряска камеры (имитация ударной волны)
        // StartCoroutine(CameraShake());

        // 4. Визуальная вспышка на весь экран (опционально)
        // StartCoroutine(ScreenFlash());
    }

    System.Collections.IEnumerator CameraShake()
    {
        Camera mainCam = Camera.main;
        Vector3 originalPos = mainCam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            mainCam.transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCam.transform.localPosition = originalPos;
    }

    System.Collections.IEnumerator ScreenFlash()
    {
        GameObject flashObj = new GameObject("ScreenFlash");
        flashObj.transform.SetParent(Camera.main.transform);
        flashObj.transform.localPosition = Vector3.zero;
        
        var image = flashObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 0.9f, 0.7f, 0.8f);
        
        float duration = 0.1f;
        float timer = duration;
        
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            float alpha = timer / duration * 0.8f;
            image.color = new Color(1f, 0.9f, 0.7f, alpha);
            yield return null;
        }
        
        Destroy(flashObj);
    }
}
