// DRV8825 pin tanımları
const int dirPin = 2;  // Yön pini (DRV8825'in DIR pinine bağlanır)
const int stepPin = 3; // Adım pini (DRV8825'in STEP pinine bağlanır)

// AccelStepper kütüphanesini dahil et
#include <AccelStepper.h>

// Motor nesnesini oluştur
// AccelStepper::DRIVER parametresi, DRV8825 gibi ayrı step ve yön pinleri olan sürücüler için kullanılır.
AccelStepper stepper(AccelStepper::DRIVER, stepPin, dirPin);

// --- HX711 - Load Cell ---
#include <HX711_ADC.h>
// ESP8266, ESP32 veya AVR (Arduino Uno, Mega vb.) için EEPROM kütüphanesini dahil et
#if defined(ESP8266)|| defined(ESP32) || defined(AVR)
#include <EEPROM.h>
#endif

const int HX711_dout = 4; // HX711'in DOUT (Data Out) pini Arduino'nun 4. dijital pinine
const int HX711_sck = 5;  // HX711'in SCK (Serial Clock) pini Arduino'nun 5. dijital pinine

HX711_ADC LoadCell(HX711_dout, HX711_sck);

const int calVal_eepromAdress = 0; // Kalibrasyon değerinin EEPROM'daki adresi

// --- HC-SR04 Ultrasonik Sensör ---
const int trigPin = 8; // HC-SR04'ün Trig pini Arduino'nun 8. dijital pinine
const int echoPin = 9; // HC-SR04'ün Echo pini Arduino'nun 9. dijital pinine

// Her iki veri (kuvvet ve mesafe) için ortak yazdırma zamanlayıcısı
unsigned long lastPrintTime = 0;
const int printInterval = 1000; // Verileri her 1000 ms'de (1 saniye) bir yazdır

// Sensör değerlerini depolamak için değişkenler
float currentKuvvet = 0.0;
float currentMesafe = 0.0;

void setup() {
  // Seri iletişimi başlat
  Serial.begin(57600);
  delay(10); 

  // --- Step Motor Ayarları ---
  stepper.setMaxSpeed(1000); 
  stepper.setAcceleration(500); 
  digitalWrite(dirPin, HIGH); // Motorun dönüş yönü
  stepper.setSpeed(500);      // Motorun sabit dönüş hızı

  // --- Load Cell Ayarları ---
  LoadCell.begin();
  // Çıktıyı tersine çevirerek basınca azalma, çekince artma davranışı sağlar
  LoadCell.setReverseOutput(); 
  
  float calibrationValue = 696.0; // Kendi kalibrasyon değerinizi buraya girin!
  
  #if defined(ESP8266)|| defined(ESP32) || defined(AVR)
  EEPROM.get(calVal_eepromAdress, calibrationValue); 
  #endif

  unsigned long stabilizingTime = 2000; // Sensörün stabilize olması için bekleme süresi (milisecond)
  boolean performTare = true;           // Başlangıçta tare (sıfırlama) işlemi yapılıp yapılmayacağı
  
  LoadCell.start(stabilizingTime, performTare);
  
  if (LoadCell.getTareTimeoutFlag()) {
    // Hata durumunda hiçbir şey yazdırmayacak, sadece burada kalacak.
    while(1); 
  } else {
    LoadCell.setCalFactor(calibrationValue); 
  }

  // --- Ultrasonik Sensör Ayarları ---
  pinMode(trigPin, OUTPUT); 
  pinMode(echoPin, INPUT);  
}

void loop() {
  // AccelStepper kütüphanesi, `setSpeed()` ile belirlenen hızda motorun sürekli dönmesini sağlar.
  // Bu fonksiyonun loop döngüsünde sürekli çağrılması zorunludur.
  stepper.runSpeed(); 

  // --- Load Cell Okuma ---
  if (LoadCell.update()) {
    float rawLoadValue = LoadCell.getData(); 
    currentKuvvet = (rawLoadValue / 1000.0) * 9.81; // Kuvveti Newton'a çevir
  }

  // --- Ultrasonik Mesafe Ölçümü ---
  long duration;    // Ses dalgasının gidiş-dönüş süresi
  
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10); 
  digitalWrite(trigPin, LOW);

  duration = pulseIn(echoPin, HIGH);
  currentMesafe = duration * 0.0343 / 2; // Mesafeyi santimetre olarak hesapla

  // --- Seri Port'a Veri Yazdırma ---
  // Belirli aralıklarla (1 saniye) kuvvet ve mesafe değerlerini tek satırda yazdır
  if (millis() - lastPrintTime >= printInterval) {
    // Kuvvet değerini tam sayıya yuvarla (veya istediğiniz ondalık basamak kadar bırakabilirsiniz)
    Serial.print( (int)(currentKuvvet + 0.5) ); // En yakın tam sayıya yuvarlama
    Serial.print("*");
    // Mesafe değerini tam sayıya yuvarla
    Serial.println( (int)((currentMesafe + 0.5)*10)); // En yakın tam sayıya yuvarlama
    
    lastPrintTime = millis(); // Son yazdırma zamanını güncelle
  }

  // Seri terminalden 't' komutu gelirse tare (sıfırlama) işlemi başlat
  // Bu kısım çıktıyı etkilemez, sadece tare işlemini yapar.
  if (Serial.available() > 0) {
    char inByte = Serial.read();
    if (inByte == 't' || inByte == 'T') { 
      LoadCell.tareNoDelay(); 
      // Tare işlemi başladığında seri porta çıktı verilmediği için kullanıcıya bilgi verilmeyecektir.
      // İsterseniz burada kısa bir bip sesi veya LED yakma gibi görsel bir geri bildirim ekleyebilirsiniz.
    }
  }
}