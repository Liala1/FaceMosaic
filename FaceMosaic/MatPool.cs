using OpenCvSharp;

namespace FaceMosaic;

public class MatBuffer
{
	public Mat? Mat { get; set; }
	public bool Usage { get; set; }
}

public class MatPool
{
	readonly MatBuffer[] buffers;
	readonly object lockObject = new ();
	readonly int poolSize;
	
	public MatPool(int poolSize)
	{
		this.poolSize = poolSize;
		buffers = new MatBuffer[poolSize];
		for (var i = 0; i < poolSize; ++i)
		{
			buffers[i] = new MatBuffer();
		}
	}
	
	
	public MatBuffer? Rent()
	{
		lock (lockObject)
		{
			foreach (var buffer in buffers)
			{
				if (!buffer.Usage)
				{
					buffer.Usage = true;
					return buffer;
				}
			}
			return null;
		}
	}

	public void Return(MatBuffer buffer)
	{
		buffer.Usage = false;
	}
}