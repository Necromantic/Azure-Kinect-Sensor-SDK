using System;
using Microsoft.Azure.Kinect.Sensor;

namespace Microsoft.Azure.Kinect.BodyTracking
{
    public class BodyTracker : IDisposable
    {
        private NativeMethods.k4abt_tracker_t handle;

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyTracker"/> class.
        /// </summary>
        /// <param name="calibration"></param>
        public BodyTracker(Calibration calibration)
        {
            AzureKinectException.ThrowIfNotSuccess(() => NativeMethods.k4abt_tracker_create(ref calibration, out this.handle));

            // Hook the native allocator and register this object.
            // .Dispose() will be called on this object when the allocator is shut down.
            Allocator.Singleton.RegisterForDisposal(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyTracker"/> class.
        /// </summary>
        /// <param name="handle"></param>
        internal BodyTracker(NativeMethods.k4abt_tracker_t handle)
        {
            // Hook the native allocator and register this object.
            // .Dispose() will be called on this object when the allocator is shut down.
            Allocator.Singleton.RegisterForDisposal(this);

            this.handle = handle;
        }


        public void EnqueueCapture(Capture capture, int timeoutInMS = -1)
        {
            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(BodyTracker));
                }

                if (capture == null)
                {
                    throw new ArgumentNullException(nameof(capture));
                }

                NativeMethods.k4a_wait_result_t result = NativeMethods.k4abt_tracker_enqueue_capture(this.handle, capture.DangerousGetHandle(), timeoutInMS);

                if (result == NativeMethods.k4a_wait_result_t.K4A_WAIT_RESULT_TIMEOUT)
                {
                    throw new TimeoutException("Timed out waiting for capture");
                }

                AzureKinectException.ThrowIfNotSuccess(() => result);
            }
        }

        public BodyFrame PopFrame(int timeoutInMS = -1)
        {
            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(BodyTracker));
                }

                NativeMethods.k4a_wait_result_t result = NativeMethods.k4abt_tracker_pop_result(this.handle, out NativeMethods.k4abt_frame_t frameHandle, timeoutInMS);

                if (result == NativeMethods.k4a_wait_result_t.K4A_WAIT_RESULT_TIMEOUT)
                {
                    throw new TimeoutException("Timed out waiting for body frame");
                }

                AzureKinectException.ThrowIfNotSuccess(() => result);

                if (frameHandle.IsInvalid)
                {
                    throw new AzureKinectException("k4abt_tracker_pop_result did not return a valid body frame handle");
                }

                return new BodyFrame(frameHandle);
            }
        }
        
        public void Shutdown()
        {
            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(BodyTracker));
                }

                NativeMethods.k4abt_tracker_shutdown(this.handle);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue && disposing) {
                Allocator.Singleton.UnregisterForDisposal(this);

                this.handle.Close();
                this.handle = null;

                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BodyTracker"/> class.
        /// </summary>
        ~BodyTracker()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
