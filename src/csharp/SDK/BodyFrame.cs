using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Azure.Kinect.Sensor;

namespace Microsoft.Azure.Kinect.BodyTracking
{
    public class BodyFrame : IDisposable
    {
        public const byte BodyIndexMapBackground = 255;
        public const uint InvalidBodyID = 0xFFFFFFFF;

        private NativeMethods.k4abt_frame_t handle;

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyFrame"/> class.
        /// </summary>
        /// <param name="handle"></param>
        internal BodyFrame(NativeMethods.k4abt_frame_t handle)
        {
            // Hook the native allocator and register this object.
            // .Dispose() will be called on this object when the allocator is shut down.
            Allocator.Singleton.RegisterForDisposal(this);

            this.handle = handle;
        }

        public uint GetNumBodies()
        {
            return NativeMethods.k4abt_frame_get_num_bodies(this.handle);
        }

        [StructLayout(LayoutKind.Sequential)]
        [Sensor.Native.NativeReference("k4abt_joint_t")]
        public struct Joint
        {
            public Vector3 position;
            public Quaternion orientation;
        }

        public enum Joints
        {
            PELVIS = 0,
            SPINE_NAVAL,
            SPINE_CHEST,
            NECK,
            CLAVICLE_LEFT,
            SHOULDER_LEFT,
            ELBOW_LEFT,
            WRIST_LEFT,
            CLAVICLE_RIGHT,
            SHOULDER_RIGHT,
            ELBOW_RIGHT,
            WRIST_RIGHT,
            HIP_LEFT,
            KNEE_LEFT,
            ANKLE_LEFT,
            FOOT_LEFT,
            HIP_RIGHT,
            KNEE_RIGHT,
            ANKLE_RIGHT,
            FOOT_RIGHT,
            HEAD,
            NOSE,
            EYE_LEFT,
            EAR_LEFT,
            EYE_RIGHT,
            EAR_RIGHT,
            COUNT
        }


        [StructLayout(LayoutKind.Sequential)]
        [Sensor.Native.NativeReference("k4abt_skeleton_t")]
        public struct Skeleton
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = (int)Joints.COUNT)]
            public Joint[] joints;
        }

        [StructLayout(LayoutKind.Sequential)]
        [Sensor.Native.NativeReference("k4abt_body_t")]
        public struct Body
        {
            public uint id;
            public Skeleton skeleton;
        }

        public Skeleton GetBodySkeleton(uint index)
        {
            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(BodyTracker));
                }

                Skeleton skeleton = default;
                AzureKinectException.ThrowIfNotSuccess(() => NativeMethods.k4abt_frame_get_body_skeleton(this.handle, index, out skeleton));

                return skeleton;
            }
        }

        public uint GetBodyID(uint index)
        {
            if (this.disposedValue)
            {
                throw new ObjectDisposedException(nameof(BodyTracker));
            }

            return NativeMethods.k4abt_frame_get_body_id(this.handle, index);
        }

        public Image GetBodyIndexMap()
        {
            if (this.disposedValue)
            {
                throw new ObjectDisposedException(nameof(BodyTracker));
            }

            return new Image(NativeMethods.k4abt_frame_get_body_index_map(this.handle));
        }

        public Capture GetCapture()
        {
            if (this.disposedValue)
            {
                throw new ObjectDisposedException(nameof(BodyTracker));
            }

            return new Capture(NativeMethods.k4abt_frame_get_capture(this.handle));
        }

        public TimeSpan Timestamp
        {
            get
            {
                lock (this)
                {
                    if (this.disposedValue)
                    {
                        throw new ObjectDisposedException(nameof(BodyFrame));
                    }

                    ulong timestamp = NativeMethods.k4abt_frame_get_timestamp_usec(this.handle);
                    return TimeSpan.FromTicks(checked((long)timestamp) * 10);
                }
            }
        }

        public BodyFrame Reference()
        {
            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(BodyFrame));
                }

                return new BodyFrame(this.handle.DuplicateReference());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue && disposing)
            {
                Allocator.Singleton.UnregisterForDisposal(this);

                this.handle.Close();
                this.handle = null;

                this.disposedValue = true;
            }
        }

        ~BodyFrame()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
