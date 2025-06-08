using System;
using System.Runtime.InteropServices;
using UnityEngine; // Added for Debug.LogError if needed

namespace DenoiserPlugin
{
    public class RingBufferAllocator : IDisposable
    {
        private readonly byte[] _buffer;
        private readonly int _capacity;
        private int _writePosition;
        private readonly GCHandle _gcHandle;
        private readonly IntPtr _bufferPtr;

        // TODO: Add synchronization primitives if C# allocates from multiple threads,
        // or if C++ reads while C# writes to the same segment.
        // For now, assume C# calls to Allocate are serialized (e.g., all from render thread).

        public RingBufferAllocator(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            _capacity = capacity;
            _buffer = new byte[capacity];
            _writePosition = 0;
            
            _gcHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            _bufferPtr = _gcHandle.AddrOfPinnedObject();

            // TODO: Log or ensure _bufferPtr is successfully passed to the C++ side 
            // via a dedicated PInvoke call like `InitializeSharedRingBuffer(IntPtr bufferPtr, int capacity)`.
        }

        /// <summary>
        /// Allocates space in the ring buffer for the given struct and returns a direct native pointer to it.
        /// Assumes the C++ side knows the size of the struct.
        /// </summary>
        /// <typeparam name="T">The type of the struct to store. Must be a struct.</typeparam>
        /// <param name="item">The struct instance to store.</param>
        /// <returns>A native pointer to the location within the ring buffer where the data is stored. 
        /// Returns IntPtr.Zero if allocation fails.</returns>
        public IntPtr Allocate<T>(T item) where T : struct
        {
            int dataLength = Marshal.SizeOf(item);
            int requiredTotalSize = dataLength; // Size for serialized struct data

            // TODO: Implement robust ring buffer logic with read pointers to prevent overwriting unconsumed data.
            // This V1 simplified version focuses on basic allocation and wrapping.
            // It assumes C++ consumes data fast enough or the buffer is large enough.

            if (requiredTotalSize > _capacity)
            {
                // Data is larger than the entire buffer. Cannot allocate.
                // TODO: Log this error. Consider throwing an exception.
                Debug.LogError($"[RingBufferAllocator] Allocation failed: Required size ({requiredTotalSize} bytes) for struct {typeof(T).Name} exceeds buffer capacity ({_capacity} bytes).");
                return IntPtr.Zero; // Indicate allocation failure
            }

            int allocatedOffset;

            // Check if there's enough contiguous space at the current write position
            if (_writePosition + requiredTotalSize <= _capacity)
            {
                allocatedOffset = _writePosition;
            }
            else
            {
                // Not enough space at the end, try to wrap to the beginning.
                // TODO: This is a very simple wrap. A real ring buffer would need to know the read position
                // to ensure we are not overwriting unread data.
                // For V1, assume wrapping is okay if there's space at the beginning AND data fits.
                if (requiredTotalSize <= _capacity) // Check if it fits if we start from 0
                {
                     // We can only wrap if the data fits from the beginning.
                    // The check for overwriting unread data is a crucial TODO for a robust implementation.
                    _writePosition = 0; 
                    allocatedOffset = _writePosition; // Start writing from the beginning
                }
                else
                {
                    // This case should theoretically be caught by the initial (requiredTotalSize > _capacity) check.
                    // This is a defensive return.
                    Debug.LogError("[RingBufferAllocator] Allocation failed: Insufficient space even after attempting to wrap (this should be rare).");
                    return IntPtr.Zero; 
                }
            }
            
            // Final check after potential wrap to ensure contiguous space from allocatedOffset
            if (allocatedOffset + requiredTotalSize > _capacity)
            {
                Debug.LogError("[RingBufferAllocator] Allocation failed: No contiguous block of sufficient size available.");
                return IntPtr.Zero; // Indicate allocation failure
            }

            // At this point, allocatedOffset is where we will write.
            IntPtr structDataDestinationInPinnedArray = (IntPtr)(_bufferPtr.ToInt64() + allocatedOffset);
            Marshal.StructureToPtr(item, structDataDestinationInPinnedArray, false); // Write struct directly to pinned memory offset
            // Note: We are not using Marshal.Copy here from a temporary native pointer anymore.
            // We directly write to the offset in the _buffer via its pinned _bufferPtr for the struct part.
            // For the struct part, an alternative to Marshal.StructureToPtr to the `structDataDestinationInPinnedArray`
            // would be to serialize to a temporary byte array first, then Buffer.BlockCopy that.
            // Marshal.StructureToPtr(item, tempStructPtr, true);
            // Marshal.Copy(tempStructPtr, _buffer, allocatedOffset, dataLength);
            // The current direct StructureToPtr is more efficient if it works reliably for all blittable types.

            // Update write position for the next allocation
            _writePosition = allocatedOffset + requiredTotalSize;

            // TODO: Consider alignment for _writePosition if C++ side requires it (e.g., align to 4 or 8 bytes).

            // Return the absolute pointer to the start of the data in the ring buffer
            return (IntPtr)(_bufferPtr.ToInt64() + allocatedOffset);
        }

        /// <summary>
        /// Allocates a contiguous array of elements and returns a pointer for user to fill the data
        /// </summary>
        /// <typeparam name="T">The type of elements to allocate</typeparam>
        /// <param name="count">The number of elements to allocate</param>
        /// <returns>Pointer to the allocated memory, returns IntPtr.Zero if allocation fails</returns>
        public IntPtr AllocateArray<T>(int count) where T : unmanaged
        {
            if (count <= 0)
            {
                Debug.LogError("[RingBufferAllocator] Invalid count: must be positive.");
                return IntPtr.Zero;
            }

            int elementSize = Marshal.SizeOf<T>();
            int requiredTotalSize = elementSize * count;

            if (requiredTotalSize > _capacity)
            {
                Debug.LogError($"[RingBufferAllocator] Allocation failed: Required size ({requiredTotalSize} bytes) for {count} elements of {typeof(T).Name} exceeds buffer capacity ({_capacity} bytes).");
                return IntPtr.Zero;
            }

            int allocatedOffset;

            // Check if there's enough contiguous space at the current write position
            if (_writePosition + requiredTotalSize <= _capacity)
            {
                allocatedOffset = _writePosition;
            }
            else
            {
                // Try to wrap to the beginning
                if (requiredTotalSize <= _capacity)
                {
                    _writePosition = 0;
                    allocatedOffset = _writePosition;
                }
                else
                {
                    Debug.LogError("[RingBufferAllocator] Allocation failed: Insufficient space even after attempting to wrap.");
                    return IntPtr.Zero;
                }
            }

            // Final check
            if (allocatedOffset + requiredTotalSize > _capacity)
            {
                Debug.LogError("[RingBufferAllocator] Allocation failed: No contiguous block of sufficient size available.");
                return IntPtr.Zero;
            }

            // Update write position for the next allocation
            _writePosition = allocatedOffset + requiredTotalSize;

            // Return the absolute pointer to the start of the allocated memory
            return (IntPtr)(_bufferPtr.ToInt64() + allocatedOffset);
        }

        /// <summary>
        /// Gets the native pointer to the beginning of the pinned buffer.
        /// This should be passed to the C++ side for it to access the ring buffer.
        /// </summary>
        public IntPtr GetBufferPointer()
        {
            return _bufferPtr;
        }
        
        public int GetCapacity()
        {
            return _capacity;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // if (disposing)
            // {
            //     // Free managed resources here if any
            // }

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
            // TODO: Inform C++ that this buffer is no longer valid, if C++ holds a raw pointer.
            // This might involve a PInvoke call to a C++ cleanup function.
        }

        ~RingBufferAllocator()
        {
            Dispose(false);
        }
    }
} 