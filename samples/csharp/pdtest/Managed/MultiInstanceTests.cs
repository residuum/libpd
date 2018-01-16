using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibPDBinding.Managed;
using LibPDBinding.Managed.Data;
using LibPDBinding.Managed.Events;
using NUnit.Framework;

namespace LibPDBindingTest.Managed
{
	[TestFixture]
	public class MultiInstanceTests
	{
		Pd _instance1;
		Pd _instance2;
		Patch _patch1;
		Patch _patch2;
		static readonly int _inputs = 2;
		static readonly int _outputs = 2;
		static readonly int _sampleRate = 44100;

		[SetUp]
		public void Init ()
		{
			_instance1 = new Pd (_inputs, _outputs, _sampleRate);
			_instance2 = new Pd (_inputs, _outputs, _sampleRate);
			_patch1 = _instance1.LoadPatch ("../../test_multi.pd");
			_patch2 = _instance2.LoadPatch ("../../test_multi.pd");
		}

		[TearDown]
		public void Cleanup ()
		{
			_patch1.Dispose ();
			_patch2.Dispose ();
			_instance1.Dispose ();
			_instance2.Dispose ();
		}

		[Test]
		public virtual void DecoupledReceiversTest ()
		{
			float value1 = 0;
			float value2 = 0;
			string receiver = "spam";
			_instance1.Messaging.Bind (receiver);
			_instance2.Messaging.Bind (receiver);
			_instance1.Messaging.Float += delegate(object sender, FloatEventArgs e) {
				if (e.Receiver == receiver) {
					value1 = e.Float.Value;
				}
			};
			_instance2.Messaging.Float += delegate(object sender, FloatEventArgs e) {
				if (e.Receiver == receiver) {
					value2 = e.Float.Value;
				}
			};
			_instance1.Messaging.Send (receiver, new Float (42));
			_instance1.Messaging.Unbind (receiver);
			_instance2.Messaging.Unbind (receiver);
			Assert.AreEqual (42, value1);
			Assert.AreEqual (0, value2);

		}

		[Test]
		public virtual void DecoupledMidiTest ()
		{
			int channel = 1;
			int pitch = 64;
			int velocity = 32;
			int receivedChannel1 = 0;
			int receivedPitch1 = 0;
			int receivedVelocity1 = 0;
			_instance1.Midi.NoteOn += delegate (object sender, NoteOnEventArgs args) {
				receivedChannel1 = args.Channel;
				receivedPitch1 = args.Pitch;
				receivedVelocity1 = args.Velocity;
			};
			int receivedChannel2 = 0;
			int receivedPitch2 = 0;
			int receivedVelocity2 = 0;
			_instance2.Midi.NoteOn += delegate (object sender, NoteOnEventArgs args) {
				receivedChannel2 = args.Channel;
				receivedPitch2 = args.Pitch;
				receivedVelocity2 = args.Velocity;
			};
			_instance1.Midi.SendNoteOn (channel, pitch, velocity);
			_instance2.Midi.SendNoteOn (channel + 1, pitch + 1, velocity + 1);
			Assert.AreEqual (channel, receivedChannel1);
			Assert.AreEqual (pitch, receivedPitch1);
			Assert.AreEqual (velocity, receivedVelocity1);            
			Assert.AreEqual (channel + 1, receivedChannel2);
			Assert.AreEqual (pitch + 1, receivedPitch2);
			Assert.AreEqual (velocity + 1, receivedVelocity2);            
		}

		[Test]
		public virtual void DecoupledArrayTest ()
		{
			int arraySize = 128;
			PdArray array1 = _instance1.GetArray ("array1");
			PdArray array2 = _instance2.GetArray ("array1");
			float[] valueToSet = new float[arraySize];
			for (int i = 0; i < arraySize; i++) {
				valueToSet [i] = 1f;
			}
			array1.Write (valueToSet, 0, arraySize);
			float[] readArray1 = array1.Read (0, arraySize);
			float[] readArray2 = array2.Read (0, arraySize);
			for (int i = 0; i < arraySize; i++) {
				Assert.AreEqual (1f, readArray1 [i]);
			}
			for (int i = 0; i < arraySize; i++) {
				Assert.AreEqual (0f, readArray2 [i]);
			}
			array2.Resize (arraySize * 2);
			Assert.AreEqual (arraySize, array1.Size);
			Assert.AreEqual (arraySize * 2, array2.Size);
		}

		[Test]
		public virtual void DecoupledAudioTest ()
		{
			int arraySize = _inputs * _instance1.BlockSize;
			float[] valueToSet1 = new float[arraySize];
			float[] valueToSet2 = new float[arraySize];
			float[] valueToGet1 = new float[arraySize];
			float[] valueToGet2 = new float[arraySize];
			for (int i = 0; i < arraySize; i++) {
				valueToSet1 [i] = 1f;
				valueToSet2 [i] = 0.5f;
			}
			_instance1.Start ();
			_instance1.Process (1, valueToSet1, valueToGet1);
			_instance1.Stop ();
			_instance2.Start ();
			_instance2.Process (1, valueToSet2, valueToGet2);
			_instance2.Stop ();
			for (int i = 0; i < arraySize; i++) {
				Assert.AreEqual (1f, valueToGet1 [i]);
			}
			for (int i = 0; i < arraySize; i++) {
				Assert.AreEqual (0.5f, valueToGet2 [i]);
			}
		}
	}
}
