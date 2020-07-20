﻿namespace EvidentInstruction.Models.Interfaces
{
    public interface IFileProvider
    {
        bool CheckFileExtension(string filename);
        bool Exist(string path);
        bool AppendAllText(string filename, string path, string content);
        bool Create(string filename, string path, string content);

        bool WriteAllText(string filename, string path, string content);
        bool Delete(string fullpath);
    }
}