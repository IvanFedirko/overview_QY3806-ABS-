using System;
using System.Data;
using System.Device.Spi;

namespace EncoderQY3806
{
    class Program
    {
        static void Main(string[] args)
        {

            var spiConf = new SpiConnectionSettings(0, 0); // SPI = 0, CS = 0
            spiConf.ChipSelectLineActiveState = 0; //Активное состояние чип селекта 0
            spiConf.ClockFrequency = 80000; // 80Mhz частота
            spiConf.Mode = SpiMode.Mode1; //Полярность устанавливается на низком, и выборка данных осуществляется по заднему фронту тактового сигнала.


            SpiDevice spi = SpiDevice.Create(spiConf);

            while (true)
            {
                Console.SetCursorPosition(0, 0); //идем в начало
                Console.Write("\f\u001bc\x1b[3J");//чистим консоль

                cmd_8021(spi);
                System.Threading.Thread.Sleep(2); //пауза 2мс

            }


        }

        static void cmd_8021(SpiDevice spi)
        {
            Console.WriteLine("Команда 0x8021");

            byte[] writeBuffer = new byte[6] { 0x80, 0x21, 0x00, 0x00, 0x00, 0x00 }; //пишем 6ть байт
            byte[] readBuffer = new byte[6]; //читаем 6ть байт
            spi.TransferFullDuplex(writeBuffer, readBuffer); //Пишем и читаем

            ushort angleData = BitConverter.ToUInt16(new byte[2] { readBuffer[3], readBuffer[2] }); //данные угла
            ushort maskAngleData = 0x7FFF; //выделяем нужные биты
            double del = 360 / 32768.0; // 360 град 15 бит(32768 точек)
            var printAngle = Math.Round((angleData & maskAngleData) * del, 2); //округляем до 2х знаков после запятой

            ushort safetyWord = BitConverter.ToUInt16(new byte[2] { readBuffer[5], readBuffer[4] }); //получаем  проверочное слово

            safetyWord = (ushort)(safetyWord & 0x0fff); //выделяем нужную информацию по маске

            Console.WriteLine($"Угол: {printAngle}, Статус: {(safetyWord >> 8).ToString("X")},  CRC8: {(safetyWord & 0xff).ToString("X")} ");

            //заполняем массив для проверки полученных значений методом контрольной суммы
            byte[] crcCheckArray = new byte[6];
            crcCheckArray[0] = 0x80;
            crcCheckArray[1] = 0x21;
            crcCheckArray[2] = (byte)(angleData >> 8);
            crcCheckArray[3] = (byte)(angleData & 0xff);
            crcCheckArray[4] = (byte)(safetyWord >> 8);
            crcCheckArray[5] = (byte)(safetyWord & 0xff);

            var crc8 = Crc8.Calculate(crcCheckArray, 4);

            Console.WriteLine($"a[0]= {crcCheckArray[0].ToString("X")}, a[1]= {crcCheckArray[1].ToString("X")}, a[2]= {crcCheckArray[2].ToString("X")}, a[3]= {crcCheckArray[3].ToString("X")}, CRC8(calc) = {crc8.ToString("X")}");
            Console.WriteLine($"Контрольные суммы равны: {crc8 == (safetyWord & 0xff)}");

        }

      
    }
}
