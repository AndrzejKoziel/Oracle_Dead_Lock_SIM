delete from scn_results sr
where sr.ID IN ('41342732','41342299')
and sr.STATUS = 'IO' and sr.EQ_HMI_ID = :inEqHmiId