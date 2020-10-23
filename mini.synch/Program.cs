using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace mini.synch
{
    class Program
    {
        static void Main(string[] args)
        {


            foreach (var item in Directory.GetFiles("c:\\temp\\run\\"))
                File.Delete(item);

            var tblFile = File.ReadAllLines(@"C:\temp\cm.manager\cm_tbl_mng_202010060113.csv");
            List<TblClass> tblList = new List<TblClass>();
            foreach (var item in tblFile.Skip(1))
            {
                tblList.Add(new TblClass()
                {
                    tbl = item.Split(',')[0],
                    datetime = DateTime.Parse(item.Split(',')[1]),
                    ossid = int.Parse(item.Split(',')[2]),
                    cnt = int.Parse(item.Split(',')[3]),

                });
            }

            int offset = 0;
            ProcessENM(tblList, offset);
            ProcessHWI(tblList, offset);
            ProcessERI(tblList, offset);
        }

        private static void ProcessHWI(List<TblClass> tblList, int offset)
        {
            string finalUqery = "";

            StringBuilder sb = new StringBuilder();
            //421
            //423
            //422
            //424
            //425
            //HWI
            var tblHWIObj = tblList.Where(a => (a.ossid == 421 || a.ossid == 422 || a.ossid == 423 || a.ossid == 424 || a.ossid == 425) && a.datetime.Date == DateTime.Now.Date.AddDays(offset)).ToList();
            string query = "";

            var tblHWI = tblHWIObj.Select(a => a.tbl).Distinct();

            sb.AppendLine("truncate table synch_tree_hwi;");
            sb.AppendLine("truncate table synch_data_hwi;");
            List<string> fileList = new List<string>();

            foreach (var item in tblHWI.Where(a => a.Contains("tree")))
            {
                Console.WriteLine(item);

                query = $@"insert into synch_tree_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from {item} tbl
inner join cm_all_cells cac on cac.basestation  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1) and  
substring (vsmoname ,POSITION (vsmoname ,':')+1,LENGTH (vsmoname ) - POSITION (vsmoname ,':'))=cast (cac.localcellid  as String)
where motype  like 'BTS3900,NE,ENODEBFUNCTION,CELL%' and cm_all_cells.clid  = 322;";
                sb.AppendLine(query);
                sb.AppendLine();

                query = $@"insert into synch_tree_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from {item} tbl
inner join cm_all_cells cac on cac.node  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1) and  
 substring(vsmoname ,POSITION(vsmoname ,'CELLID:')+7,LENGTH(vsmoname)-POSITION(vsmoname ,'CELLID:')+7) =cac.ci  
where  motype like 'BSC6900UMTSNE,BSC6910UMTSFunction,RNCBASIC,UFLEXUEGROUPPRIO,UCELL,CELLSELRESEL'  and cm_all_cells.clid  = 321;";
                sb.AppendLine(query); sb.AppendLine();

                query = $@"insert into synch_tree_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,320 clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from {item} tbl
inner join mnp.cm_all_cells_hwi_glocell2  cac on cac.nename  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1) and  
substring (vsmoname ,POSITION (vsmoname ,':')+1,LENGTH (vsmoname ) - POSITION (vsmoname ,':'))=cast (cac.glocellid  as String)
where  vsmoname  like '%/GLOCELL%' ;";
                sb.AppendLine(query); sb.AppendLine();


                query = $@"insert into synch_tree_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from  {item}  tbl
inner join cm_all_cells cac on cac.node  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1)  and 
toString(cac.ci  )=   reverse(SUBSTRING(reverse(vsmoname), 1 , POSITION (reverse(vsmoname), '=')-1 )) 
where   motype in ( 'BSC6900UMTSNE,BSC6900UMTSFunction,UNODEB,UCELL', 'BSC6900UMTSNE,BSC6910UMTSFunction,URNCBASIC,UNODEB,UCELL');";
                sb.AppendLine(query); sb.AppendLine();

                query = $@"insert into synch_tree_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from  {item}  tbl
inner join cm_all_cells cac on cac.node  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1)  and 
((toString(cac.hwi_cellindex  )=   reverse(SUBSTRING(reverse(vsmoname), 1 , POSITION (reverse(vsmoname), '=')-1 )) ))
where   motype in ('BSC6910GSMNE,BSC6910GSMFunction,BTS,GCELL', 'BSC6900GSMNE,BSC6900GSMFunction,BTS,GCELL' );";
                sb.AppendLine(query); sb.AppendLine();


                if (sb.Length > 50000)
                {
                    fileList.Add(sb.ToString());
                    sb.Clear();
                }
            }
            // sb.AppendLine("--####################################");

            if (sb.Length > 0)
            {
                fileList.Add(sb.ToString());
                sb.Clear();
            }


            // finalUqery = $@"clickhouse-client -h 10.167.44.10 --port 9000  --max_insert_threads=8  --max_insert_block_size=104857600 --min_insert_block_size_rows=104857600   -d mnp -n -m --query=""{sb.ToString()}"" >> run.log ";
            //  sb.Clear();

            // File.WriteAllText("c:\\temp\\run_hwi_p1.sh", finalUqery);


            foreach (var item in tblHWI.Where(a => a.Contains("data")))
            {
                Console.WriteLine(item);


                query = $@"insert into synch_data_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue 
from  {item}  tbl
inner join cm_all_cells cac on cac.basestation  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1) and  
substring (vsmoname ,POSITION (vsmoname ,':')+1,LENGTH (vsmoname ) - POSITION (vsmoname ,':'))=cast (cac.localcellid  as String)
where motype  like  'BTS3900,NE,ENODEBFUNCTION,CELL%' and cm_all_cells.clid  = 322;";
                sb.AppendLine(query);
                sb.AppendLine();

                query = $@"insert into synch_data_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue 
from  {item}  tbl
inner join cm_all_cells cac on cac.node  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1) and  
 substring(vsmoname ,POSITION(vsmoname ,'CELLID:')+7,LENGTH(vsmoname)-POSITION(vsmoname ,'CELLID:')+7) =cac.ci  
where  motype like 'BSC6900UMTSNE,BSC6910UMTSFunction,RNCBASIC,UFLEXUEGROUPPRIO,UCELL,CELLSELRESEL'  and cm_all_cells.clid  = 321;";
                sb.AppendLine(query);
                sb.AppendLine();

                query = $@"insert into synch_data_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,320 clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue 
from  {item}  tbl
inner join mnp.cm_all_cells_hwi_glocell2  cac on cac.nename  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1) and  
substring (vsmoname ,POSITION (vsmoname ,':')+1,LENGTH (vsmoname ) - POSITION (vsmoname ,':'))=cast (cac.glocellid  as String)
where  vsmoname  like '%/GLOCELL%';";
                sb.AppendLine(query);
                sb.AppendLine();

                query = $@"insert into synch_data_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue 
from  {item}  tbl
inner join cm_all_cells cac on cac.node  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1)  and 
toString(cac.ci  )=   reverse(SUBSTRING(reverse(vsmoname), 1 , POSITION (reverse(vsmoname), '=')-1 )) 
where   motype in ( 'BSC6900UMTSNE,BSC6900UMTSFunction,UNODEB,UCELL', 'BSC6900UMTSNE,BSC6910UMTSFunction,URNCBASIC,UNODEB,UCELL');";
                sb.AppendLine(query);
                sb.AppendLine();

                query = $@"insert into synch_data_hwi  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue 
from  {item}  tbl
inner join cm_all_cells cac on cac.node  =  substring(vsmoname,1, POSITION (vsmoname ,'/')-1)  and 
((toString(cac.hwi_cellindex  )=   reverse(SUBSTRING(reverse(vsmoname), 1 , POSITION (reverse(vsmoname), '=')-1 )) ))
where   motype in ('BSC6910GSMNE,BSC6910GSMFunction,BTS,GCELL', 'BSC6900GSMNE,BSC6900GSMFunction,BTS,GCELL' );";

                sb.AppendLine(query);
                sb.AppendLine();

                sb.AppendLine(query); sb.AppendLine();
                if (sb.Length > 50000)
                {
                    fileList.Add(sb.ToString());
                    sb.Clear();
                }

            }
            sb.AppendLine(query); sb.AppendLine();
            if (sb.Length > 50000)
            {
                fileList.Add(sb.ToString());
                sb.Clear();
            }
            //  sb.AppendLine("-- ##########################");
            // sb.AppendLine("-- LOAD TO MAIN");

            //  finalUqery = $@"clickhouse-client -h 10.167.44.10 --port 9000  --max_insert_threads=8  --max_insert_block_size=104857600 --min_insert_block_size_rows=104857600   -d mnp -n -m --query=""{sb.ToString()}"" >> run.log";
            //  sb.Clear();

            //  File.WriteAllText("c:\\temp\\run_hwi_p2.sh", finalUqery);


            query = $@"insert into vs_cm_tree (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype from synch_tree_hwi;";
            sb.AppendLine(query);

            foreach (var item in tblHWI.Where(a => a.Contains("tree")))
            {
                query = $@"insert into vs_cm_tree (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype from {item} where motype not in (select  distinct motype from synch_tree_hwi );";
                sb.AppendLine(query);
            }

            query = $@"insert into vs_cm_data (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from synch_data_hwi;";
            sb.AppendLine(query);

            foreach (var item in tblHWI.Where(a => a.Contains("data")))
            {
                query = $@"insert into vs_cm_data (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from {item} where motype not in (select  distinct motype from synch_data_hwi );";
                sb.AppendLine(query);
            }

            sb.AppendLine("-- ##########################");
            sb.AppendLine("-- RENAMING TABLES TO X_ tables that is done");

            foreach (var item in tblHWI)
            {
                query = $@"rename table  {item}  to x_{item};";
                sb.AppendLine(query);
            }


            finalUqery = $@"clickhouse-client -h 10.167.44.10 --port 9000  --max_insert_threads=8  --max_insert_block_size=10485760000 --min_insert_block_size_rows=10485760000   -d mnp -n -m --query=""{sb.ToString()}"" >> run.log";

            File.WriteAllText("c:\\temp\\run\\finalHWI.sh", finalUqery);

            string command = " time bash run_enm_p1.sh &&  time bash run_eri_p1.sh  ";
            int i = 1;
            foreach (var item in fileList)
            {
                finalUqery = $@"clickhouse-client -h 10.167.44.10 --port 9000  --max_insert_threads=8  --max_insert_block_size=10485760000 --min_insert_block_size_rows=10485760000   -d mnp -n -m --query=""{item.ToString()}"" >> run.log";
                File.WriteAllText($"c:\\temp\\run\\hwistep{i}.sh", finalUqery);
                command += $"&& time bash hwistep{i}.sh ";
                i++;
            }
            command += " && time bash finalHWI.sh";
            Console.WriteLine("########");
            Console.WriteLine(command);
            Console.WriteLine("########");
            Console.WriteLine(@"c:\temp\run\");

        }

        private static void ProcessENM(List<TblClass> tblList, int offset)
        {
            StringBuilder sb = new StringBuilder();
            //ENM
            //193
            //192
            //194
            //191
            var tblENM = tblList.Where(a => (a.ossid == 191 || a.ossid == 192 || a.ossid == 193 || a.ossid == 194) && 
            a.datetime.Date == DateTime.Now.Date.AddDays(offset)).ToList();
            string query = "";

            sb.AppendLine("truncate table synch_tree;");
            foreach (var item in tblENM.Where(a => a.tbl.Contains("tree")))
            {
                Console.WriteLine(item);
                query = $@"insert into synch_tree  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from  {item.tbl} vs inner join cm_all_cells cac on cac.cell = reverse(SUBSTRING(reverse(vs.vsmoname), 1 , POSITION (reverse(vs.vsmoname), '=')-1 )) where motype = 'ManagedElement,vsDataRncFunction,vsDataUtranCell';";
                sb.AppendLine(query);
                query = $@"insert into synch_tree  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from  {item.tbl} vs inner join cm_all_cells cac on cac.cell = reverse(SUBSTRING(reverse(vs.vsmoname), 1 , POSITION (reverse(vs.vsmoname), '=')-1 )) where motype = 'ManagedElement,vsDataENodeBFunction,vsDataEUtranCellFDD';";
                sb.AppendLine(query);
            }
            sb.AppendLine("--####################################");
            sb.AppendLine("truncate table synch_data;");
            foreach (var item in tblENM.Where(a => a.tbl.Contains("data")))
            {
                Console.WriteLine(item.tbl);
                query = $@"insert into synch_data  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from {item.tbl} vs inner join cm_all_cells cac on cac.cell = reverse(SUBSTRING(reverse(vs.vsmoname), 1 , POSITION (reverse(vs.vsmoname), '=')-1 ))
where motype = 'ManagedElement,vsDataENodeBFunction,vsDataEUtranCellFDD';";
                sb.AppendLine(query);
                query = $@"insert into synch_data  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from {item.tbl} vs inner join cm_all_cells cac on cac.cell = reverse(SUBSTRING(reverse(vs.vsmoname), 1 , POSITION (reverse(vs.vsmoname), '=')-1 )) 
where motype = 'ManagedElement,vsDataRncFunction,vsDataUtranCell';";
                sb.AppendLine(query);
            }

            sb.AppendLine("-- ##########################");
            sb.AppendLine("-- LOAD TO MAIN");

            query = $@"insert into vs_cm_tree (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype)
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype from synch_tree;";
            sb.AppendLine(query);


            foreach (var item in tblENM.Where(a => a.tbl.Contains("tree")))
            {
                query = $@"insert into vs_cm_tree (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype from {item.tbl} where motype not in (select  distinct motype from synch_tree );";
                sb.AppendLine(query);
            }

            query = $@"insert into vs_cm_data (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from synch_data;";
            sb.AppendLine(query);

            foreach (var item in tblENM.Where(a => a.tbl.Contains("data")))
            {
                query = $@"insert into vs_cm_data (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from {item.tbl} where motype not in (select  distinct motype from synch_data );";
                sb.AppendLine(query);
            }

            sb.AppendLine("-- ##########################");
            sb.AppendLine("-- RENAMING TABLES TO X_ tables that is done");

            foreach (var item in tblENM.Where(a => a.tbl.Contains("tree")))
            {
                query = $@"rename table  {item.tbl}  to x_{item.tbl};";
                sb.AppendLine(query);
            }


            foreach (var item in tblENM.Where(a => a.tbl.Contains("data")))
            {
                query = $@"rename table  {item.tbl}  to x_{item.tbl};";
                sb.AppendLine(query);
            }

            string finalUqery = $@"clickhouse-client -h 10.167.44.10 --port 9000  --max_insert_threads=8  --max_insert_block_size=10485760000 --min_insert_block_size_rows=10485760000   -d mnp -n -m --query=""{sb.ToString()}"" >> run.log ";


            File.WriteAllText("c:\\temp\\run\\run_enm_p1.sh", finalUqery);
        }

        private static void ProcessERI(List<TblClass> tblList, int offset)
        {
            StringBuilder sb = new StringBuilder();
            //ERI
            //125
            var tblENM = tblList.Where(a => (a.ossid == 125) && a.datetime.Date == DateTime.Now.Date.AddDays(offset)).ToList();
            string query = "";

            sb.AppendLine("truncate table synch_tree;");
            foreach (var item in tblENM.Where(a => a.tbl.Contains("tree")))
            {
                Console.WriteLine(item);
                query = $@"insert into synch_tree  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype ) 
select datadatetime ,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype  
from  {item.tbl} vs inner join cm_all_cells cac on splitByChar(':',vs.vsmoname)[2] = cac.cell and splitByChar(':',vs.vsmoname)[1] = cac.node;";
                sb.AppendLine(query);
            }
            sb.AppendLine("--####################################");
            sb.AppendLine("truncate table synch_data;");
            foreach (var item in tblENM.Where(a => a.tbl.Contains("data")))
            {
                Console.WriteLine(item.tbl);
                query = $@"insert into synch_data  (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue) 
select datadatetime,cac.cellid pk1,pk2,pk3,pk4,cac.clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue 
from {item.tbl} vs inner join cm_all_cells cac on splitByChar(':',vs.vsmoname)[2] = cac.cell and splitByChar(':',vs.vsmoname)[1] = cac.node;";

                sb.AppendLine(query);
            }

            sb.AppendLine("-- ##########################");
            sb.AppendLine("-- LOAD TO MAIN");

            query = $@"insert into vs_cm_tree (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype from synch_tree;";
            sb.AppendLine(query);


            foreach (var item in tblENM.Where(a => a.tbl.Contains("tree")))
            {
                query = $@"insert into vs_cm_tree (datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype) 
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid, treeelementclass ,netopologyfolder ,treedepth ,parentpimoname ,pimoname ,vsmoname ,displayvsmoname ,motype from {item.tbl} where motype not in (select  distinct motype from synch_tree );";
                sb.AppendLine(query);
            }

            query = $@"insert into vs_cm_data (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue)
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from synch_data;";
            sb.AppendLine(query);

            foreach (var item in tblENM.Where(a => a.tbl.Contains("data")))
            {
                query = $@"insert into vs_cm_data (datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue)
select datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue from {item.tbl} where motype not in (select  distinct motype from synch_data );";
                sb.AppendLine(query);
            }

            sb.AppendLine("-- ##########################");
            sb.AppendLine("-- RENAMING TABLES TO X_ tables that is done");

            foreach (var item in tblENM.Where(a => a.tbl.Contains("tree")))
            {
                query = $@"rename table  {item.tbl}  to x_{item.tbl};";
                sb.AppendLine(query);
            }


            foreach (var item in tblENM.Where(a => a.tbl.Contains("data")))
            {
                query = $@"rename table  {item.tbl}  to x_{item.tbl};";
                sb.AppendLine(query);
            }

            string finalUqery = $@"clickhouse-client -h 10.167.44.10 --port 9000  --max_insert_threads=8  --max_insert_block_size=10485760000 --min_insert_block_size_rows=10485760000   -d mnp -n -m --query=""{sb.ToString()}"" >> run.log ";


            File.WriteAllText("c:\\temp\\run\\run_eri_p1.sh", finalUqery);
        }


        public class TblClass
        {
            public string tbl { get; set; }
            public DateTime datetime { get; set; }
            public int ossid { get; set; }

            public int cnt { get; set; }

        }
    }
}
