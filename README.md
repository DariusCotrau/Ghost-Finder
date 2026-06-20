# 👻 Ghost Finder

Un joc horror hide & seek realizat în Unity 6, inspirat din mecanicile clasice de ghost hunting. Jucătorul trebuie să exploreze harta bântuita, să folosească echipamente speciale pentru a detecta prezența unei entități paranormale și să o prindă.

---

## 📝 Descrierea Jocului
În **Ghost Finder**, ești un investigator paranormal trimis în locații bântuite pentru a scăpa de fantoma care stă acolo. Fantomă este implicit invizibilă și poate fi detectată doar prin utilizarea strategică a gadget-urilor din dotare. Tensiunea crește pe măsură ce explorezi camerele întunecate, gestionând resursele limitate, cum ar fi bateria lanternei UV.

---

## 🕹️ Controale (Cum te miști)
Mișcarea de bază a personajului folosește configurația clasică de PC:

* **`W` `A` `S` `D`** - Deplasare (Înainte / Stânga / Înapoi / Dreapta)
* **`Space`** - Săritură (Jump)
* **`Mouse`** - Privire în jur (Look Around)

---

## 🛠️ Ghid de Echipament (Cum folosești fiecare item)

Jocul pune la dispoziție o serie de unelte unice pentru investigație. Fiecare item are o mecanică specifică:

### 💡 Lanterna de pe Cap (Headlight)
* **Tastă activare:** **`F`**
* **Funcționalitate:** Oferă o lumină ambientală constantă pentru a naviga prin casă. Nu consumă baterie și este atașată permanent de jucător.

### 🟣 Lanterna UV (UV Flashlight)
* **Tastă activare:** **Click Stânga** (atunci când este echipată)
* **Funcționalitate:** Proiectează o rază de lumină ultravioletă capabilă să **dezvăluie fantoma invizibilă**. 
* **⚠️ Mecanică de Nerf (Managementul Bateriei):** Lanterna UV poate fi folosită continuu timp de maximum **5 secunde**. Dacă se descarcă complet, sistemul intră în cooldown și are nevoie de **10 secunde** pentru o reîncărcare completă la 100%. Un slider pe UI îți arată în timp real nivelul bateriei.

### 🔊 Senzorul de Mișcare (Motion Sensor)
* **Funcționalitate:** Un dispozitiv plasat pe podea care monitorizează o zonă circulară. În momentul în care o entitate (jucătorul sau fantoma) intră în raza sa de detecție, senzorul începe să **bipaie ritmic**, avertizând echipa de activitate.

### 📟 EMF Reader *(În dezvoltare)*
* **Funcționalitate:** Dispozitiv de măsurare a câmpului electromagnetic care va detecta distanța și intensitatea prezenței fantomei prin activarea treptată a celor 5 LED-uri specifice și accelerarea semnalului sonor.

---

## 🚀 Mecanici Tehnice Implementate
Dacă ești evaluator și te uiți peste cod, iată principalele sisteme programate în C#:
* **Sistem de Render Pipeline (URP):** Materiale optimizate pentru Unity 6 cu tehnici de transparență controlate din cod pentru apariția fantomei (fără artefacte de Alpha Sorting/X-Ray).
* **Fizică și Detecție prin Raycasting:** Lanterna UV trimite raze dinamice pe Layer-uri specifice (`GhostLayer`) pentru a modifica proprietățile shader-elor în timp real.
* **Sistem de Triggere Proporționale:** Senzorul de mișcare folosește logica de `OnTriggerEnter` / `OnTriggerExit` combinată cu componente `Rigidbody` cinematice pentru acuratețe maximă.
* **UI Responsiv:** Interfață dinamică (baterie, texte) ancorată corect pentru a fi compatibilă cu orice rezoluție de ecran.

---

## 🛠️ Tehnologii Folosite
* **Motor grafic:** Unity 6 (LTS)
* **Render Pipeline:** Universal Render Pipeline (URP)
* **Limbaj de programare:** C#
* **Versiune Git LFS:** Folosită pentru stocarea asset-urilor 3D și audio mari.

---

## 👥 Echipa
* **Botiz Alexandru-Gabriel** 
* **Bărbăscu Raul** 
* **Cotrău Darius**
* **Burbea Alexandru**
