using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using DPFP.Verification;
using HuellaDigital.Model;

namespace HuellaDigital
{
    public partial class Form1 : Form, DPFP.Capture.EventHandler
    {
        private Capture Captura;
        private Enrollment Enroller;
        private Template template;
        private Verification verificador;

        private delegate void delegadoMuestra(string text);

        public Form1()
        {
            InitializeComponent();
        }

        //mettodo para el conteo de veces a tocar el lector
        private void mostrarVeces(string texto)
        {
            if (lblPasarDedo.InvokeRequired)
            {
                delegadoMuestra deleg = new delegadoMuestra(mostrarVeces);

                this.Invoke(deleg, new object[] {texto });
            }
            else{
                lblPasarDedo.Text = texto;
            }

            if(Enroller.FeaturesNeeded == 0)
            {
                btnGuardar.Enabled = true;
                txtNombre.Enabled = true;
            }
        }

        protected virtual void Init()
        {
            try
            {
                Captura = new Capture();
                
                if(Captura != null)
                {
                    Captura.EventHandler = this;
                    Enroller = new Enrollment();
                    verificador = new Verification();
                    template = new Template();
                    StringBuilder text = new StringBuilder();
                    text.AppendFormat("Necesita pasar el dedo {0} veces", Enroller.FeaturesNeeded);
                    lblPasarDedo.Text = text.ToString();
                }
                else
                {
                    MessageBox.Show("No se pudo instaciar la captura");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("No se pudo inicializar la captura");
            }
        }

        //metodo para iniciar la captura
        protected void IniciarCaptura()
        {
            if(Captura != null)
            {
                try
                {
                    Captura.StartCapture();
                }
                catch (Exception)
                {
                    MessageBox.Show("No se pudo iniciar captura");
                }
            }
        }

        //metodo para para la captura
        protected void PararCaptura()
        {
            if (Captura != null)
            {
                try
                {
                    Captura.StopCapture();
                }
                catch (Exception)
                {
                    MessageBox.Show("No se pudo parar la captura");
                }
            }
        }

        public void OnComplete(object Capture, string ReaderSerialNumber, Sample Sample)
        {
            PonerImagen(ConvertirImagenBit(Sample));

            FeatureSet caracteristicas = ExtraerCaracteristicas(Sample, DataPurpose.Verification);
            
            if(caracteristicas != null)
            {
                Huella _huella = new Huella();
                var result = new Verification.Result();

                var list  = _huella.Listar();
                bool verificado = false;
                string nombre = "";

                foreach(var l in list)
                {
                    var memoria = new MemoryStream(l.Huella1);
                    template.DeSerialize(memoria.ToArray());
                    verificador.Verify(caracteristicas, template, ref result);

                    if (result.Verified)
                    {
                        nombre = l.nombre;
                        verificado = true;
                        break;
                    }

                }

                if (verificado)
                {
                    mapNombre(nombre);
                }
                else
                {
                    Procesar(Sample);
                    //MessageBox.Show("No se encontro ningun registro");
                }

            }
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            mostrarTexto("Se ha retirado el dedo en el lector");
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            mostrarTexto("Se ha colocado el dedo en el lector");
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            mostrarTexto("El lector esta conecado");
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            mostrarTexto("el lector esta desconectado");
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, CaptureFeedback CaptureFeedback)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Init();
            IniciarCaptura();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            PararCaptura();
        }


        //metodo para convertir la imagen a secuencia de bits
        protected Bitmap ConvertirImagenBit(Sample sample)
        {
            SampleConversion convertidor = new SampleConversion();
            Bitmap mapaBits = null;
            convertidor.ConvertToPicture(sample, ref mapaBits);
            return mapaBits; 
        }

        //metodo para poner imagen en el pictureBox
        private void PonerImagen(Image bmp)
        {
            ImagenHuella.Image = bmp;
        }

        //metodo para extraer las caracteristicas capturadas por la huella
        protected FeatureSet ExtraerCaracteristicas(Sample sample, DataPurpose purpose)
        {
            FeatureExtraction extractor = new FeatureExtraction();
            CaptureFeedback alimentacion = CaptureFeedback.None;

            FeatureSet caracteristicas = new FeatureSet();

            extractor.CreateFeatureSet(sample, purpose, ref alimentacion, ref caracteristicas);

            if (alimentacion == CaptureFeedback.Good)
            {
                return caracteristicas;
            }
            else
            {
                return null;
            }
        }

        //meotodo para procesar las caracteristicas
        protected void Procesar(Sample sample)
        {
            FeatureSet caracteristicas = ExtraerCaracteristicas(sample, DataPurpose.Enrollment);
            if(caracteristicas != null)
            {
                try
                {
                    Enroller.AddFeatures(caracteristicas);

                }
                catch
                {
                    throw;
                }
                finally
                {
                    StringBuilder text = new StringBuilder();
                    text.AppendFormat("Necesita pasar el dedo {0} veces", Enroller.FeaturesNeeded);
                    mostrarVeces(text.ToString());
                    switch (Enroller.TemplateStatus)
                    {
                        case Enrollment.Status.Ready:
                            template = Enroller.Template;
                            mostrarTexto("La huella a sido capturada");
                            PararCaptura();
                            break;

                        case Enrollment.Status.Failed:
                            Enroller.Clear();
                            PararCaptura();
                            IniciarCaptura();
                            break;
                    }
                }
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            var nombre = txtNombre.Text.ToString();
            byte[] huella;

            using(var mem = new MemoryStream(template.Bytes))
            {
               huella = mem.ToArray();
            }

            var model = new Huella
            {
                nombre = nombre,
                Huella1 = huella
            };

            var resp = model.Guardar();

            if (resp)
            {
                MessageBox.Show("Huella registrada exitosamente");
                txtNombre.Clear();
                txtNombre.Enabled = false;
                btnGuardar.Enabled = false;
                Init();
                IniciarCaptura();
            }
        }

        protected void mapNombre(string nombre)
        {
            CheckForIllegalCrossThreadCalls = false;
            txtNombre.Text = nombre;
        }

        public void mostrarTexto(string mensaje)
        {
            CheckForIllegalCrossThreadCalls = false;
            txtMensaje.Text += "\r\n" + mensaje;
        }

    }
}
