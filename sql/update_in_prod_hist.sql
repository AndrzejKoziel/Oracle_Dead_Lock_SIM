update in_prod_hist iph
set STATUS=1
where iph.ID IN ('41342732','41342299')
and iph.EQ_HMI_ID = :inEqHmiId