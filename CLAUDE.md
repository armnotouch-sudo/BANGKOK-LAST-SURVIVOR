# BANGKOK: LAST SURVIVOR: กฎการทำงานของโปรเจกต์

เกมเอาชีวิตรอดจากซอมบี้ มุมกล้อง Isometric 3D ฉากกรุงเทพฯ

## เทคโนโลยีหลัก (ห้ามเปลี่ยนเองโดยไม่ได้รับอนุญาต)
- **Engine:** Unity 6 (`6000.6.7f1`)
- **ภาษา:** C# เท่านั้น
- **Render Pipeline:** URP (`com.unity.render-pipelines.universal` 17.6.0)
  - `Assets/Settings/PC_RPAsset.asset` + `PC_Renderer.asset` สำหรับ PC
  - `Assets/Settings/Mobile_RPAsset.asset` + `Mobile_Renderer.asset` สำหรับ Android
- **Input:** Input System แบบใหม่ (`com.unity.inputsystem` 1.20.1) โดย Active Input Handling = Input System Package (New)
  - **ห้ามใช้** `UnityEngine.Input` (Input Manager แบบเก่า) เพราะจะ error ตอนรัน
- **UI:** uGUI (`com.unity.ugui` 2.6.0, มี TextMeshPro รวมอยู่)
- **แพลตฟอร์มเป้าหมาย:** PC (Windows) และ Android ต้องเข้ากันได้ทั้งสองแพลตฟอร์มเสมอ

## ความเข้ากันได้ PC + Android (บังคับ)
- ฟีเจอร์ที่รับ input ต้องรองรับทั้งคีย์บอร์ด/เมาส์/เกมแพด **และ** ระบบสัมผัส (virtual joystick / ปุ่มบนจอ)
- อ่าน input ผ่านชั้นกลาง (`PlayerInputReader`) ห้ามให้ระบบเกมไปอ่านอุปกรณ์โดยตรง
- Shader/Material ต้องเป็นของ URP และต้องรันบน Mobile Renderer ได้ ห้ามใช้ฟีเจอร์ที่มีเฉพาะ PC โดยไม่มีทางเลือกสำรอง
- คำนึงถึงประสิทธิภาพบนมือถือ: หลีกเลี่ยงการสร้าง GC allocation ใน `Update`, `Find*`/`GetComponent` ทุกเฟรม และใช้ object pooling กับของที่เกิดบ่อย (ซอมบี้, กระสุน, เอฟเฟกต์)
- โค้ดเฉพาะแพลตฟอร์มให้ห่อด้วย `#if UNITY_ANDROID` / `#if UNITY_STANDALONE` และต้องมีเส้นทางสำหรับอีกแพลตฟอร์มด้วย
- UI ต้องรองรับหลายอัตราส่วนจอ และ Safe Area บนมือถือ
- ข้อความภาษาไทยต้องแสดงผลได้ (ดู `ThaiFontProvider`)

## มาตรฐานโค้ด
- โค้ดของเกมทั้งหมดอยู่ใต้ `Assets/_Project/`
- Namespace ขึ้นต้นด้วย `BangkokLastSurvivor.` ตามโฟลเดอร์ (เช่น `BangkokLastSurvivor.Player`, `BangkokLastSurvivor.UI.Touch`)
- หนึ่งไฟล์ต่อหนึ่ง class หลัก ชื่อไฟล์ตรงกับชื่อ class (PascalCase)
- ฟิลด์ที่แสดงใน Inspector ใช้ `[SerializeField] private` แทน `public`
- โค้ด Editor อยู่ในโฟลเดอร์ `Editor/` เท่านั้น ห้ามให้โค้ด runtime อ้างถึง `UnityEditor` (ยกเว้นห่อด้วย `#if UNITY_EDITOR`)
- ใช้ ScriptableObject เก็บข้อมูลค่าเกม (สถิติอาวุธ, ซอมบี้ ฯลฯ) แทนการ hard-code

## กฎความปลอดภัยในการทำงาน
- **ห้ามลบไฟล์เดิม** หากจำเป็นต้องลบหรือย้าย ให้ถามก่อน
- **ห้ามแก้ Project Settings** (`ProjectSettings/*`), `Packages/manifest.json`, Quality/Graphics settings หรือ URP assets โดยไม่ได้ขออนุญาต
- ห้ามแตะ `Library/`, `Temp/`, `Logs/`, `UserSettings/` และไฟล์ `.csproj`/`.slnx` ที่ Unity สร้างเอง
- เวลาย้าย/เปลี่ยนชื่อ asset ต้องย้ายไฟล์ `.meta` ไปด้วยเสมอ (ไม่งั้น reference ในฉากจะหาย) แนะนำให้ทำผ่าน Unity Editor
- ก่อนทำอะไรที่เสี่ยง (ลบ, ย้ายเป็นชุด, แก้ไฟล์ scene/prefab YAML โดยตรง, เพิ่ม/ลบ package) ต้องถามผู้ใช้ก่อน
- ทำเฉพาะสิ่งที่ถูกขอในแต่ละขั้น ไม่สร้างระบบเกมเกินขอบเขตที่สั่ง

## โครงสร้างโฟลเดอร์
```
Assets/
  _Project/
    Scripts/        Core, Input, Player, CameraSystem, UI, UI/Touch, Testing, ...
    Editor/         เครื่องมือ Editor (เช่น Milestone1SceneBuilder)
    Art/            Models, Materials, Textures, Animations, VFX
    Audio/          Music, SFX
    Prefabs/
    ScriptableObjects/
    Scenes/
    Fonts/
  Settings/         URP assets (ของ template — ห้ามแก้โดยไม่ขอ)
  Scenes/           ฉากเดิมจาก template + ฉากทดสอบ
```

## สถานะปัจจุบัน
- มีโค้ด Milestone 1 แล้ว: การเคลื่อนที่ผู้เล่น, กล้อง Isometric, HUD, ปุ่มสัมผัส, ซอมบี้ยังไม่มี
- เมนู `BANGKOK > Milestone 1 > Create Test Scene` สร้าง `Assets/Scenes/M1_TestScene.unity`
