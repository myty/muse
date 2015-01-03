using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muse.Web.Services
{
	public interface IFileUpdater
	{
		void FileUpdated(string file);
		void FileDeleted(string file);
	}
}
